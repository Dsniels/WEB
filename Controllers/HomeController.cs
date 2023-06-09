﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Web;

using WEB.Models;
using System.Web.Mvc;

namespace WEB.Controllers
{
    public class HomeController : Controller
    {
        static string Conexion = "data source=localhost;initial catalog=DATOS;integrated security=True;multipleactiveresultsets=True;application name=EntityFramework";
        private Models.CITA CITA = new Models.CITA();
        public ActionResult Index()
        {
            ViewBag.NombreCliente = "";

            if (Session["LoggedIn"] != null && (bool)Session["LoggedIn"])
            {
                ViewBag.Cliente = Session["Cliente"];
                ViewBag.IsLoggedIn = true;

                return View();
            }

            return View();
        }

        [HttpGet]
        public ActionResult Cita()
        {

            return View();
        }

        [HttpPost]
        public ActionResult Cita(string atencionmed, string nombre, string apellido, int? edad, String fecha, string telefono, string descripcion)
        {
            if (!(Session["LoggedIn"] != null && (bool)Session["LoggedIn"]))
            {
                return View();
            }

            DateTime fechaCita;
            if (!DateTime.TryParse(fecha, out fechaCita))
            {
                ModelState.AddModelError("fecha", "El formato de fecha es incorrecto.");

            }
            if (ModelState.IsValid) // Validar los datos 
            {
                try // Ingresar los datos
                {
                    if (string.IsNullOrEmpty(atencionmed) || string.IsNullOrEmpty(nombre) || string.IsNullOrEmpty(apellido) || string.IsNullOrEmpty(fecha) || string.IsNullOrEmpty(telefono))
                    {
                        ViewBag.alerta = "danger";
                        ViewBag.res = "Todos los campos obligatorios deben ser llenados.";
                    }
                    else
                    {

                        if (CITA.Insertar(Int32.Parse(Session["IdCliente"] + ""), atencionmed, nombre, apellido, edad ?? 0, Convert.ToDateTime(fecha), telefono, descripcion))
                        {
                            ViewBag.alerta = "success";
                            ViewBag.res = "Cita registrada exitosamente.";
                        }
                        else
                        {
                            ViewBag.alerta = "danger";
                            ViewBag.res = "Error al registrar la cita :(";
                        }
                    }
                }
                catch (Exception)
                {

                }
            }
            else
            {
                ViewBag.alerta = "danger";
                ViewBag.res = "Datos de la cita no válidos.";
            }

            return View();
        }

        [HttpGet]
        public ActionResult Mis_Citas()
        {
            if (Session["LoggedIn"] != null && (bool)Session["LoggedIn"])
            {
                var IdCliente = Int32.Parse(Session["IdCliente"] + "");
                return View(CITA.Listar(IdCliente));
            }

            return View();
        }

        [HttpGet]
        public ActionResult Editar(int id)
        {

            var cita = CITA.filtrar(id);
            return View(cita);

        }

        [HttpPost]
        public ActionResult Editar(CITA form, int id)
        {
            if (CITA.Actualizar(form, id))
            {
                ViewBag.alerta = "success";
                ViewBag.res = "Datos actualizados";
                return RedirectToAction("Mis_citas", "Home");
            }
            else
            {
                ViewBag.alerta = "danger";
                ViewBag.res = "Ocurrio un error :( ";
            }
           return View();
        }

        public ActionResult Eliminar(int id)
        {
            if (CITA.Eliminar(id))
            {
                ViewBag.alerta = "success";
                ViewBag.res = "Cita eliminada";
                return RedirectToAction("Mis_Citas", "Home");
            }
            else
            {
                ViewBag.alerta = "danger";
                ViewBag.res = "Ocurrio un error :(";
                return View(CITA.filtrar(id));
            }
        }


        public ActionResult Signup_page()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Signup_page(CLIENTE Usuario)
        {

            if (string.IsNullOrEmpty(Usuario.Nombre) || string.IsNullOrEmpty(Usuario.Apellido) || string.IsNullOrEmpty(Usuario.Correo) || string.IsNullOrEmpty(Usuario.Contraseña))
            {
                ViewBag.alerta = "danger";
                ViewBag.res = "Todos los campos son obligatorios.";
                return View();
            }

            bool Registrado;
            string Mensaje;

            using (SqlConnection cn = new SqlConnection(Conexion))
            {

                SqlCommand cmd = new SqlCommand("registrar", cn);
                cmd.Parameters.AddWithValue("Nombre", Usuario.Nombre);
                cmd.Parameters.AddWithValue("Apellido", Usuario.Apellido);
                cmd.Parameters.AddWithValue("Correo", Usuario.Correo);
                cmd.Parameters.AddWithValue("Contraseña", Usuario.Contraseña);
                cmd.Parameters.Add("Registrado", SqlDbType.Bit).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("Mensaje", SqlDbType.VarChar, 100).Direction = ParameterDirection.Output;
                cmd.CommandType = CommandType.StoredProcedure;


                cn.Open();

                cmd.ExecuteNonQuery();

                Registrado = Convert.ToBoolean(cmd.Parameters["Registrado"].Value);
                Mensaje = cmd.Parameters["Mensaje"].Value.ToString();


            }
            ViewBag.alerta = "danger";
            ViewBag.res = "Datos no válidos.";
            ViewData["Mensaje"] = Mensaje;

            if (Registrado)
            {
                Session["LoggedIn"] = true;
                Session["Cliente"] = Usuario;
                Session["IdCliente"] = Usuario.IdCliente;
                return RedirectToAction("Index", "Home");
            }
            else
            {
                return View();
            }

        }


        public ActionResult Login()
        {
            return View();
        }



        [HttpPost]
        public ActionResult Login(CLIENTE Usuario)
        {

            if (string.IsNullOrEmpty(Usuario.Correo) || string.IsNullOrEmpty(Usuario.Contraseña))
            {
                ViewData["Mensaje"] = "Por favor, ingrese un correo electrónico y contraseña.";
                return View();
            }

            using (SqlConnection cn = new SqlConnection(Conexion))
            {

                SqlCommand cmd = new SqlCommand("Validacion", cn);
                cmd.Parameters.AddWithValue("Correo", Usuario.Correo);
                cmd.Parameters.AddWithValue("Contraseña", Usuario.Contraseña);
                cmd.CommandType = CommandType.StoredProcedure;

                cn.Open();
                Usuario.IdCliente = Convert.ToInt32(cmd.ExecuteScalar().ToString());

            }

            if (Usuario.IdCliente != 0)
            {
                Session["LoggedIn"] = true;
                Session["Cliente"] = Usuario;
                Session["IdCliente"] = Usuario.IdCliente;
                return RedirectToAction("Index", "Home");
            }
            else
            {
                ViewData["Mensaje"] = "usuario no encontrado";
                return View();
            }


        }

        public ActionResult Cerrar_sesion()
        {
            Session["Cliente"] = null;
            return RedirectToAction("Index", "Home");

        }

        public ActionResult Articulo_1()
        {
            return View();
        }

        public ActionResult Articulo_2()
        {
            return View();
        }

        public ActionResult Articulo_3()
        {
            return View();
        }

    }
}