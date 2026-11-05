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
    public class AeronavesController : Controller
    {
        private AerolineaVIPContext db = new AerolineaVIPContext();

        // GET: Aeronaves
        public ActionResult Index()
        {
            var aeronave = db.Aeronave.Include(a => a.EstadoAeronave).Include(a => a.ModeloAeronave);
            return View(aeronave.ToList());
        }

        // GET: Aeronaves/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Aeronave aeronave = db.Aeronave.Find(id);
            if (aeronave == null)
            {
                return HttpNotFound();
            }
            return View(aeronave);
        }

        // GET: Aeronaves/Create
        public ActionResult Create()
        {
            ViewBag.EstadoAeronaveCodigo = new SelectList(db.EstadoAeronave, "Codigo", "Descripcion");
            ViewBag.ModeloAeronaveId = new SelectList(db.ModeloAeronave, "ModeloAeronaveId", "Fabricante");
            return View();
        }

        // POST: Aeronaves/Create
        // Para protegerse de ataques de publicacion excesiva, habilite las propiedades especificas a las que quiere enlazarse. Para obtener 
        // mas detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "AeronaveId,Matricula,ModeloAeronaveId,EstadoAeronaveCodigo")] Aeronave aeronave)
        {
            if (ModelState.IsValid)
            {
                if (db.Aeronave.Any(a => a.Matricula == aeronave.Matricula))
                {
                    ModelState.AddModelError("Matricula", "La matricula " + aeronave.Matricula + " ya esta registrada en otra aeronave.");
                }
                else
                {
                    try
                    {
                        db.Aeronave.Add(aeronave);
                        db.SaveChanges();
                        TempData["Mensaje"] = "Aeronave creada correctamente.";
                        return RedirectToAction("Index");
                    }
                    catch (DbUpdateException)
                    {
                        ModelState.AddModelError("", "No se pudo guardar la aeronave. Revise los datos ingresados.");
                    }
                }
            }

            ViewBag.EstadoAeronaveCodigo = new SelectList(db.EstadoAeronave, "Codigo", "Descripcion", aeronave.EstadoAeronaveCodigo);
            ViewBag.ModeloAeronaveId = new SelectList(db.ModeloAeronave, "ModeloAeronaveId", "Fabricante", aeronave.ModeloAeronaveId);
            return View(aeronave);
        }

        // GET: Aeronaves/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Aeronave aeronave = db.Aeronave.Find(id);
            if (aeronave == null)
            {
                return HttpNotFound();
            }
            ViewBag.EstadoAeronaveCodigo = new SelectList(db.EstadoAeronave, "Codigo", "Descripcion", aeronave.EstadoAeronaveCodigo);
            ViewBag.ModeloAeronaveId = new SelectList(db.ModeloAeronave, "ModeloAeronaveId", "Fabricante", aeronave.ModeloAeronaveId);
            return View(aeronave);
        }

        // POST: Aeronaves/Edit/5
        // Para protegerse de ataques de publicacion excesiva, habilite las propiedades especificas a las que quiere enlazarse. Para obtener 
        // mas detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "AeronaveId,Matricula,ModeloAeronaveId,EstadoAeronaveCodigo")] Aeronave aeronave)
        {
            if (ModelState.IsValid)
            {
                if (db.Aeronave.Any(a => a.Matricula == aeronave.Matricula && a.AeronaveId != aeronave.AeronaveId))
                {
                    ModelState.AddModelError("Matricula", "La matricula " + aeronave.Matricula + " ya esta registrada en otra aeronave.");
                }
                else
                {
                    try
                    {
                        db.Entry(aeronave).State = EntityState.Modified;
                        db.SaveChanges();
                        TempData["Mensaje"] = "Aeronave actualizada correctamente.";
                        return RedirectToAction("Index");
                    }
                    catch (DbUpdateException)
                    {
                        ModelState.AddModelError("", "No se pudo actualizar la aeronave. Revise los datos ingresados.");
                    }
                }
            }
            ViewBag.EstadoAeronaveCodigo = new SelectList(db.EstadoAeronave, "Codigo", "Descripcion", aeronave.EstadoAeronaveCodigo);
            ViewBag.ModeloAeronaveId = new SelectList(db.ModeloAeronave, "ModeloAeronaveId", "Fabricante", aeronave.ModeloAeronaveId);
            return View(aeronave);
        }

        // GET: Aeronaves/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Aeronave aeronave = db.Aeronave.Find(id);
            if (aeronave == null)
            {
                return HttpNotFound();
            }
            return View(aeronave);
        }

        // POST: Aeronaves/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Aeronave aeronave = db.Aeronave.Find(id);
            db.Aeronave.Remove(aeronave);
            db.SaveChanges();
            return RedirectToAction("Index");
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
