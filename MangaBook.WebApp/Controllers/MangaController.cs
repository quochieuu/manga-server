using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MangaBook.Data.DataContext;
using MangaBook.Data.Entities;

namespace MangaBook.WebApp.Controllers
{
    [Route("")]
    public class MangaController : Controller
    {
        private readonly DataDbContext _context;

        public MangaController(DataDbContext context)
        {
            _context = context;
        }

        [Route("truyen-moi-nhat")]
        public async Task<IActionResult> Index()
        {
            return View(await _context.Manga.ToListAsync());
        }

        [Route("{slug}")]
        public async Task<IActionResult> Details(string slug)
        {
            if (slug == null)
            {
                return NotFound();
            }

            var manga = await _context.Manga
                .FirstOrDefaultAsync(m => m.Slug == slug);

            var listGenreByManga = await (from m in _context.Manga
                                    join mg in _context.MangaInGenres on m.Id equals mg.MangaId
                                    join g in _context.Genres on mg.GenreSlug equals g.Slug
                                    where m.Slug == slug
                                    select new Genre() {
                                        Name = g.Name,
                                        Description = g.Description,
                                        Slug = g.Slug,
                                        ModifiedDate = g.ModifiedDate
                                    })
                                    .ToListAsync();

            ViewBag.ListGenreByManga = listGenreByManga;

            var listChapterByManga = await (from m in _context.Manga
                                            join ch in _context.Chapters on m.Id equals ch.MangaId
                                            where m.Slug == slug
                                            select new Chapter() { 
                                                Id = ch.Id,
                                                Name = ch.Name,
                                                Slug = ch.Slug,
                                                Number = ch.Number,
                                                ModifiedDate = ch.ModifiedDate
                                            })
                                            .ToListAsync();
            ViewBag.ListChapterByManga = listChapterByManga;

            if (manga == null)
            {
                return NotFound();
            }

            return View(manga);
        }

        [Route("chap-{chapterSlug}")]
        public async Task<IActionResult> ChapterDetail(string chapterSlug)
        {
            if (chapterSlug == null)
            {
                return NotFound();
            }

            var chapter = await _context.Chapters.FirstOrDefaultAsync(p => p.Slug == chapterSlug);

            if (chapter == null)
            {
                return NotFound();
            }

            return View(chapter);
        }


    }
}
