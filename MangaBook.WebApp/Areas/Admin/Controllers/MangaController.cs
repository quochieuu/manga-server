using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MangaBook.Data.DataContext;
using MangaBook.Data.Entities;
using System.Security.Claims;
using MangaBook.Data.Helpers;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Authorization;

namespace MangaBook.WebApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("admin/manga")]
    [Authorize(Roles = "Admin")]
    public class MangaController : Controller
    {
        private readonly DataDbContext _context;

        public MangaController(DataDbContext context)
        {
            _context = context;
        }

        [Route("index")]
        [Route("")]
        public async Task<IActionResult> Index()
        {
            return View(await _context.Manga.Where(p => p.IsDeleted == false).ToListAsync());
        }

        [Route("{id}")]
        public async Task<IActionResult> ListChapters(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var chap = await _context.Chapters
                .Where(m => m.MangaId == id).ToListAsync();

            var manga = await _context.Manga
                .FirstOrDefaultAsync(m => m.Id == id);

            ViewBag.MangaId = manga.Id;
            ViewBag.MangaName = manga.Name;

            if (chap == null)
            {
                return NotFound();
            }

            return View(chap);
        }

        [Route("detail/{id}")]
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var manga = await _context.Manga
                .FirstOrDefaultAsync(m => m.Id == id);
            if (manga == null)
            {
                return NotFound();
            }

            return View(manga);
        }

        [Route("create-chapter-{mangaId}")]
        public IActionResult CreateChapter(Guid? mangaId)
        {
            if (mangaId == null)
            {
                return NotFound();
            }

            var manga = _context.Manga.FirstOrDefault(m => m.Id == mangaId);

            ViewBag.MangaId = manga.Id;
            ViewBag.MangaName = manga.Name;
            if (manga == null)
            {
                return NotFound();
            }

            return View();
        }

        [Route("create-chapter-{mangaId}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateChapter(Chapter chapter, Guid mangaId)
        {
            if (ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var slug = StringHelper.UpperToLower(StringHelper.ToUnsignString(chapter.Name));

                var createItem = new Chapter()
                {
                    Name = chapter.Name,
                    Number = chapter.Number,
                    Slug = slug,
                    Content = chapter.Content,
                    CreatedBy = Guid.Parse(userId),
                    CreatedDate = DateTime.Now,
                    ModifiedBy = Guid.Parse(userId),
                    ModifiedDate = DateTime.Now,
                    IsActive = true,
                    IsDeleted = false,
                    MangaId = mangaId
                };
                _context.Add(createItem);
                await _context.SaveChangesAsync();

                return Redirect("/admin/manga/"+ mangaId);
            }
            return View(chapter);
        }



        [Route("create")]
        public IActionResult Create()
        {
            return View();
        }

        [Route("create")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Manga manga, IFormFile files, string genre)
        {
            if (ModelState.IsValid)
            {

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var slug = StringHelper.UpperToLower(StringHelper.ToUnsignString(manga.Name));

                // Thêm ảnh vào wwwroot 
                var fileName = Path.GetFileName(files.FileName);
                var myUniqueFileName = Convert.ToString(Guid.NewGuid());
                var fileExtension = Path.GetExtension(fileName);
                var newFileName = String.Concat(myUniqueFileName, fileExtension);

                var filepath =
        new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads/manga")).Root + $@"\{newFileName}";

                using (FileStream fs = System.IO.File.Create(filepath))
                {
                    files.CopyTo(fs);
                    fs.Flush();
                }


                var newImageName = newFileName;

                var createItem = new Manga()
                {
                    Name = manga.Name,
                    Description = manga.Description,
                    Slug = slug,
                    Author = manga.Author,
                    ReleaseYear = manga.ReleaseYear,
                    MangaStatus = manga.MangaStatus,
                    IsHot = manga.IsHot,
                    UrlImage = newImageName.ToString(),
                    CreatedBy = Guid.Parse(userId),
                    CreatedDate = DateTime.Now,
                    ModifiedBy = Guid.Parse(userId),
                    ModifiedDate = DateTime.Now,
                    IsActive = true,
                    IsDeleted = false,
                    Status = 1
                };
                _context.Add(createItem);
                await _context.SaveChangesAsync();

                if (!string.IsNullOrEmpty(genre))
                {
                    // Tách chuỗi nhập vào
                    string[] genresList = genre.Split(',');

                    foreach (var genreItem in genresList)
                    {
                        // Kiểm tra genre có trùng hay không, có thì bỏ qua, không thì thêm vào bảng Genre
                        var existedGenre = this.CheckGenre(genreItem);
                        var genreId = StringHelper.ToUnsignString(genreItem);

                        if (!existedGenre)
                        {
                            // Thêm genre bào bảng Genre
                            this.InsertGenre(genreId, genreItem, Guid.Parse(userId));
                        }
                        // Truyền 2 tham số vào MangaInGenre(MangaId, GenreId);
                        this.CreateMangaGenre(createItem.Id, genreId);
                    }
                }


                return RedirectToAction(nameof(Index));
            }
            return View(manga);
        }

        // Kiểm tra genre đã tồn tại trong database hay không?
        [Route("checkgenre/{genre}")]
        public bool CheckGenre(string genre)
        {
            return _context.Genres.Count(x => x.Name.ToLower() == genre.ToLower()) > 0;
        }

        [Route("insert-genre")]
        // Thêm genre bào bảng Genre
        public void InsertGenre(string id, string name, Guid userId)
        {
            var genre = new Genre();
            genre.Id = new Guid();
            genre.Slug = id;
            genre.Name = name;
            genre.CreatedBy = userId;
            genre.ModifiedBy = userId;
            genre.CreatedDate = DateTime.Now;
            genre.ModifiedDate = DateTime.Now;
            genre.IsActive = true;
            genre.IsDeleted = false;
            _context.Genres.Add(genre);
            _context.SaveChanges();
        }

        [Route("create-manga-genre")]
        // Truyền 2 tham số vào MangaInGenre(MangaId, GenreId);
        public void CreateMangaGenre(Guid mangaId, string genreSlug)
        {
            var mangaGenre = new MangaInGenre();
            mangaGenre.MangaId = mangaId;
            mangaGenre.GenreSlug = genreSlug;

            _context.MangaInGenres.Add(mangaGenre);
            _context.SaveChanges();

        }


        [Route("edit/{id}")]
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var manga = await _context.Manga.FindAsync(id);
            if (manga == null)
            {
                return NotFound();
            }
            return View(manga);
        }

        [Route("edit/{id}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Name,Description,Slug,UrlImage,Author,ReleaseYear,IsHot,MangaStatus,Status,CreatedBy,ModifiedBy,CreatedDate,ModifiedDate,IsActive,IsDeleted")] Manga manga)
        {
            if (id != manga.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(manga);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MangaExists(manga.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(manga);
        }

        [Route("delete/{id}")]
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var manga = await _context.Manga
                .FirstOrDefaultAsync(m => m.Id == id);
            if (manga == null)
            {
                return NotFound();
            }

            return View(manga);
        }

        [Route("edit/{id}")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var manga = await _context.Manga.FindAsync(id);
            _context.Manga.Remove(manga);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MangaExists(Guid id)
        {
            return _context.Manga.Any(e => e.Id == id);
        }
    }
}
