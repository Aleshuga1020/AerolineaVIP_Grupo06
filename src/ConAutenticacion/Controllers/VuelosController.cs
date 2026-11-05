using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using ConAutenticacion.Models;

namespace ConAutenticacion.Controllers
{
    [Authorize(Roles = "Administrador,SuperUsuario")]
    public class VuelosController : Controller
    {
        private AerolineaVIPContext db = new AerolineaVIPContext();

        // GET: Vuelos
        public ActionResult Index()
        {
            var vuelo = db.Vuelo.Include(v => v.Aeronave).Include(v => v.EstadoVuelo).Include(v => v.Frecuencia);
            return View(vuelo.ToList());
        }

        // GET: Vuelos/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Vuelo vuelo = db.Vuelo.Find(id);
            if (vuelo == null)
            {
                return HttpNotFound();
            }
            return View(vuelo);
        }

        // GET: Vuelos/Create
        public ActionResult Create()
        {
            ViewBag.AeronaveId = new SelectList(db.Aeronave, "AeronaveId", "Matricula");
            ViewBag.EstadoVueloCodigo = new SelectList(db.EstadoVuelo, "Codigo", "Descripcion");
            ViewBag.FrecuenciaId = new SelectList(db.Frecuencia, "FrecuenciaId", "FrecuenciaId");
            return View();
        }

        // POST: Vuelos/Create
        // Para protegerse de ataques de publicacion excesiva, habilite las propiedades especificas a las que quiere enlazarse. Para obtener 
        // mas detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "VueloId,FrecuenciaId,FechaOperacion,AeronaveId,EstadoVueloCodigo")] Vuelo vuelo)
        {
            if (ModelState.IsValid)
            {
                var error = ValidarVuelo(vuelo);
                if (error == null)
                {
                    try
                    {
                        db.Vuelo.Add(vuelo);
                        db.SaveChanges();
                        TempData["Mensaje"] = "Vuelo creado correctamente.";
                        return RedirectToAction("Index");
                    }
                    catch (DbUpdateException)
                    {
                        error = "Ya existe un vuelo con esa frecuencia y fecha de operacion.";
                    }
                }
                ModelState.AddModelError("", error);
            }

            ViewBag.AeronaveId = new SelectList(db.Aeronave, "AeronaveId", "Matricula", vuelo.AeronaveId);
            ViewBag.EstadoVueloCodigo = new SelectList(db.EstadoVuelo, "Codigo", "Descripcion", vuelo.EstadoVueloCodigo);
            ViewBag.FrecuenciaId = new SelectList(db.Frecuencia, "FrecuenciaId", "FrecuenciaId", vuelo.FrecuenciaId);
            return View(vuelo);
        }

        // GET: Vuelos/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Vuelo vuelo = db.Vuelo.Find(id);
            if (vuelo == null)
            {
                return HttpNotFound();
            }
            ViewBag.AeronaveId = new SelectList(db.Aeronave, "AeronaveId", "Matricula", vuelo.AeronaveId);
            ViewBag.EstadoVueloCodigo = new SelectList(db.EstadoVuelo, "Codigo", "Descripcion", vuelo.EstadoVueloCodigo);
            ViewBag.FrecuenciaId = new SelectList(db.Frecuencia, "FrecuenciaId", "FrecuenciaId", vuelo.FrecuenciaId);
            return View(vuelo);
        }

        // POST: Vuelos/Edit/5
        // Para protegerse de ataques de publicacion excesiva, habilite las propiedades especificas a las que quiere enlazarse. Para obtener 
        // mas detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "VueloId,FrecuenciaId,FechaOperacion,AeronaveId,EstadoVueloCodigo")] Vuelo vuelo)
        {
            if (ModelState.IsValid)
            {
                var error = ValidarVuelo(vuelo);
                if (error == null)
                {
                    try
                    {
                        db.Entry(vuelo).State = EntityState.Modified;
                        db.SaveChanges();
                        TempData["Mensaje"] = "Vuelo actualizado correctamente.";
                        return RedirectToAction("Index");
                    }
                    catch (DbUpdateException)
                    {
                        error = "Ya existe un vuelo con esa frecuencia y fecha de operacion.";
                    }
                }
                ModelState.AddModelError("", error);
            }
            ViewBag.AeronaveId = new SelectList(db.Aeronave, "AeronaveId", "Matricula", vuelo.AeronaveId);
            ViewBag.EstadoVueloCodigo = new SelectList(db.EstadoVuelo, "Codigo", "Descripcion", vuelo.EstadoVueloCodigo);
            ViewBag.FrecuenciaId = new SelectList(db.Frecuencia, "FrecuenciaId", "FrecuenciaId", vuelo.FrecuenciaId);
            return View(vuelo);
        }

        // GET: Vuelos/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Vuelo vuelo = db.Vuelo.Find(id);
            if (vuelo == null)
            {
                return HttpNotFound();
            }
            return View(vuelo);
        }

        // POST: Vuelos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Vuelo vuelo = db.Vuelo.Find(id);
            db.Vuelo.Remove(vuelo);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        private static readonly string[] NombresDia = { "", "lunes", "martes", "miercoles", "jueves", "viernes", "sabado", "domingo" };

        private static int DiaIso(DateTime fecha)
        {
            return fecha.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)fecha.DayOfWeek;
        }

        // Reglas de negocio del vuelo: frecuencia activa, dia de la semana (RN-04), aeronave disponible (RN-06) y sin solapamiento.
        private string ValidarVuelo(Vuelo vuelo)
        {
            var frecuencia = db.Frecuencia.Find(vuelo.FrecuenciaId);
            if (frecuencia == null) return "Seleccione una frecuencia valida.";
            if (!frecuencia.Activa) return "La frecuencia seleccionada no esta activa.";
            if (frecuencia.DiaSemana != DiaIso(vuelo.FechaOperacion))
            {
                return "La fecha " + vuelo.FechaOperacion.ToString("dd/MM/yyyy") + " es " + NombresDia[DiaIso(vuelo.FechaOperacion)]
                     + ", pero la frecuencia opera los " + NombresDia[frecuencia.DiaSemana] + ".";
            }
            if (vuelo.AeronaveId.HasValue)
            {
                var aeronave = db.Aeronave.Find(vuelo.AeronaveId.Value);
                if (aeronave == null) return "La aeronave seleccionada no existe.";
                if (aeronave.EstadoAeronaveCodigo == "NO_DISPONIBLE")
                    return "La aeronave " + aeronave.Matricula + " esta NO_DISPONIBLE y no puede asignarse a vuelos.";
                if (vuelo.EstadoVueloCodigo != "CANCELADO")
                {
                    var frecuenciasOtros = db.Vuelo
                        .Where(v => v.AeronaveId == vuelo.AeronaveId && v.FechaOperacion == vuelo.FechaOperacion
                                 && v.VueloId != vuelo.VueloId && v.EstadoVueloCodigo != "CANCELADO")
                        .Select(v => v.FrecuenciaId)
                        .ToList();
                    foreach (var fid in frecuenciasOtros)
                    {
                        var otra = db.Frecuencia.Find(fid);
                        if (otra != null && frecuencia.HoraSalida < otra.HoraLlegada && otra.HoraSalida < frecuencia.HoraLlegada)
                        {
                            return "La aeronave " + aeronave.Matricula + " ya tiene un vuelo entre "
                                 + otra.HoraSalida.ToString(@"hh\:mm") + " y " + otra.HoraLlegada.ToString(@"hh\:mm") + " ese dia.";
                        }
                    }
                }
            }
            return null;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
