using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TropiNailsPro.Data;
using TropiNailsPro.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;

// COMPRESION IMAGEN
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace TropiNailsPro.Controllers
{
    public class ModelosUnasController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ModelosUnasController(
            AppDbContext context,
            IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        private int ObtenerUsuarioId()
        {
            var id =
                HttpContext.Session
                .GetInt32("UsuarioId");

            if (id.HasValue)
                return id.Value;

            var claim =
                User.FindFirst(
                    "UsuarioId"
                );

            if (
                claim != null &&
                int.TryParse(
                    claim.Value,
                    out int claimId
                )
            )
                return claimId;

            return 0;
        }

private int ObtenerManicuristaId()
{
    var id =
        HttpContext.Session
        .GetInt32("ManicuristaId");

    return id ?? 0;
}

        private bool EsManicurista()
        {
            return ObtenerUsuarioId() > 0;
        }

        private bool EsDuenio(
    int manicuristaId
)
{
    return
        ObtenerManicuristaId()
        == manicuristaId;
}

        //======================================================
        // INDEX
        //======================================================

        public async Task<IActionResult> Index()
{
    int manicuristaId =
        ObtenerManicuristaId();

    ViewBag.EsManicurista =
        EsManicurista();

    ViewBag.UsuarioId =
        ObtenerUsuarioId();

    var modelos =
        await _context.ModelosUnas
        .Where(
            m =>
            m.ManicuristaId
            == manicuristaId
        )
        .ToListAsync();

    return View(modelos);
}

        //======================================================
        // CREATE
        //======================================================

        public IActionResult Create()
        {
            if (!EsManicurista())
                return RedirectToAction(
                    "Index"
                );

            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>
        Create(
            ModeloUnas modelo
        )
        {
            if(!EsManicurista())
                return RedirectToAction(
                    "Index"
                );

            if(!ModelState.IsValid)
                return View(modelo);

            modelo.ManicuristaId =
    ObtenerManicuristaId();

            await GuardarImagen(
                modelo
            );

            if(!ModelState.IsValid)
                return View(modelo);

            _context.Add(modelo);

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Index)
            );
        }


        //======================================================
        // EDIT
        //======================================================

        public async Task<IActionResult>
        Edit(int id)
        {
            var modelo=
                await _context
                .ModelosUnas
                .FindAsync(id);

            if(
                modelo==null
                ||
                !EsDuenio(
                    modelo.ManicuristaId
                )
            )
                return RedirectToAction(
                    nameof(Index)
                );

            return View(modelo);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>
        Edit(
            int id,
            ModeloUnas modelo
        )
        {
            if(id!=modelo.Id)
                return NotFound();

            var original=
                await _context
                .ModelosUnas
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x=>x.Id==id
                );

            if(
                original==null
                ||
                !EsDuenio(
                    original.ManicuristaId
                )
            )
            return RedirectToAction(
                nameof(Index)
            );

            modelo.ManicuristaId=
                original.ManicuristaId;

            await GuardarImagen(
                modelo,
                original.ImagenUrl
            );

            if(!ModelState.IsValid)
                return View(modelo);

            _context.Update(modelo);

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Index)
            );
        }



        //======================================================
        // DELETE (SIN VISTA FEA)
        //======================================================

        public async Task<IActionResult>
        Delete(int id)
        {
            var modelo=
                await _context
                .ModelosUnas
                .FindAsync(id);

            if(
                modelo==null
                ||
                !EsDuenio(
                    modelo.ManicuristaId
                )
            )
            return RedirectToAction(
                nameof(Index)
            );

            BorrarImagenLocal(
                modelo.ImagenUrl
            );

            _context.Remove(modelo);

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Index)
            );
        }



        //======================================================
        // GUARDAR IMAGEN O VIDEO
        //======================================================

        private async Task GuardarImagen(
            ModeloUnas modelo,
            string? anterior=null
        )
        {
            string carpeta=
                Path.Combine(
                    _env.WebRootPath,
                    "uploads",
                    "modelos"
                );

            if(
                !Directory.Exists(
                    carpeta
                )
            )
            Directory.CreateDirectory(
                carpeta
            );



            if(
                modelo.ImagenArchivo!=null
                &&
                modelo.ImagenArchivo
                .Length>0
            )
            {
                string extension=
                    Path.GetExtension(
                        modelo
                        .ImagenArchivo
                        .FileName
                    ).ToLower();


                string[] imagenes=
                {
                    ".jpg",
                    ".jpeg",
                    ".png",
                    ".webp"
                };


                string[] videos=
                {
                    ".mp4",
                    ".mov",
                    ".webm"
                };



                if(
                    !imagenes.Contains(
                        extension
                    )
                    &&
                    !videos.Contains(
                        extension
                    )
                )
                {
                    ModelState.AddModelError(
                        "",
                        "Solo JPG PNG WEBP MP4 MOV WEBM"
                    );

                    modelo.ImagenUrl=
                        anterior
                        ??
                        "/uploads/default-modelo.png";

                    return;
                }



                //==================================
                // VIDEO
                //==================================

                if(
                    videos.Contains(
                        extension
                    )
                )
                {
                    string nombreVideo=
                        Guid.NewGuid()
                        +
                        extension;

                    string rutaVideo=
                        Path.Combine(
                            carpeta,
                            nombreVideo
                        );

                    using(
                        var stream=
                        new FileStream(
                            rutaVideo,
                            FileMode.Create
                        )
                    )
                    {
                        await modelo
                        .ImagenArchivo
                        .CopyToAsync(
                            stream
                        );
                    }

                    BorrarImagenLocal(
                        anterior
                    );

                    modelo.ImagenUrl=
                        "/uploads/modelos/"
                        +
                        nombreVideo;

                    modelo.TipoMedia=
                        "video";

                    return;
                }




                //==================================
                // IMAGEN (COMPRESION ORIGINAL)
                //==================================

                string nombre=
                    Guid.NewGuid()
                    + ".jpg";

                string ruta=
                    Path.Combine(
                        carpeta,
                        nombre
                    );


                using var image=
                    await Image.LoadAsync(
                        modelo
                        .ImagenArchivo
                        .OpenReadStream()
                    );


                image.Mutate(
                    x=>x.Resize(
                        new ResizeOptions
                        {
                            Mode=
                            ResizeMode.Max,

                            Size=
                            new Size(
                                1000,
                                1000
                            )
                        }
                    )
                );


                await image.SaveAsync(
                    ruta,
                    new JpegEncoder
                    {
                        Quality=75
                    }
                );


                BorrarImagenLocal(
                    anterior
                );


                modelo.ImagenUrl=
                    "/uploads/modelos/"
                    +
                    nombre;

                modelo.TipoMedia=
                    "imagen";

                return;
            }



            //==================================
            // URL EXTERNA
            //==================================

            if(
                !string.IsNullOrWhiteSpace(
                    modelo.ImagenUrl
                )
                &&
                (
                    modelo.ImagenUrl.StartsWith(
                        "http://"
                    )
                    ||
                    modelo.ImagenUrl.StartsWith(
                        "https://"
                    )
                )
            )
            {
                modelo.TipoMedia=
                    "imagen";

                return;
            }


            modelo.ImagenUrl=
                anterior
                ??
                "/uploads/default-modelo.png";
        }




        //======================================================
        // BORRAR ARCHIVO LOCAL
        //======================================================

        private void BorrarImagenLocal(
            string? url
        )
        {
            if(
                string.IsNullOrEmpty(
                    url
                )
            )
            return;

            if(
                url.StartsWith(
                    "http"
                )
            )
            return;


            string ruta=
                Path.Combine(
                    _env.WebRootPath,
                    url.TrimStart('/')
                    .Replace(
                        "/",
                        Path.DirectorySeparatorChar
                        .ToString()
                    )
                );


            if(
                System.IO.File.Exists(
                    ruta
                )
            )
            {
                System.IO.File.Delete(
                    ruta
                );
            }
        }

    }
}