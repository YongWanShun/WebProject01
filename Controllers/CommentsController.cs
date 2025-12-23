using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebProject.Models;

namespace WebProject.Controllers
{
    [Authorize]
    public class CommentsController : Controller
    {
        private readonly WebProjectContext _context;

        public CommentsController(WebProjectContext context)
        {
            _context = context;
        }

        // GET: Comments
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Index()
        {
            // 驗證：檢查使用者是否登入
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Users");
            }

            // 驗證：檢查使用者是否為管理員
            if (!User.IsInRole("admin"))
            {
                return Content("<script>alert('權限不足，只有管理員可以查看所有留言！'); window.location.href='/';</script>", "text/html; charset=utf-8");
            }

            var webProjectContext = _context.Comments.Include(c => c.Parent).Include(c => c.Post).Include(c => c.User);
            return View(await webProjectContext.ToListAsync());
        }

        // GET: Comments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var comment = await _context.Comments
                .Include(c => c.Parent)
                .Include(c => c.Post)
                .Include(c => c.User)
                .FirstOrDefaultAsync(m => m.CommentId == id);
            if (comment == null)
            {
                return NotFound();
            }

            return View(comment);
        }

        // GET: Comments/Create
        public IActionResult Create()
        {
            ViewData["ParentId"] = new SelectList(_context.Comments, "CommentId", "CommentId");
            ViewData["PostId"] = new SelectList(_context.Posts, "PostId", "PostId");
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "UserId");
            return View();
        }

        // POST: Comments/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CommentId,PostId,UserId,ParentId,Content,CreatedAt,IsDeleted")] Comment comment)
        {
            if (ModelState.IsValid)
            {
                _context.Add(comment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ParentId"] = new SelectList(_context.Comments, "CommentId", "CommentId", comment.ParentId);
            ViewData["PostId"] = new SelectList(_context.Posts, "PostId", "PostId", comment.PostId);
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "UserId", comment.UserId);
            return View(comment);
        }

        // GET: Comments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var comment = await _context.Comments.FindAsync(id);
            if (comment == null)
            {
                return NotFound();
            }
            ViewData["ParentId"] = new SelectList(_context.Comments, "CommentId", "CommentId", comment.ParentId);
            ViewData["PostId"] = new SelectList(_context.Posts, "PostId", "PostId", comment.PostId);
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "UserId", comment.UserId);
            return View(comment);
        }

        // POST: Comments/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CommentId,PostId,UserId,ParentId,Content,CreatedAt,IsDeleted")] Comment comment)
        {
            if (id != comment.CommentId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(comment);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CommentExists(comment.CommentId))
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
            ViewData["ParentId"] = new SelectList(_context.Comments, "CommentId", "CommentId", comment.ParentId);
            ViewData["PostId"] = new SelectList(_context.Posts, "PostId", "PostId", comment.PostId);
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "UserId", comment.UserId);
            return View(comment);
        }

        // GET: Comments/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var comment = await _context.Comments
                .Include(c => c.Parent)
                .Include(c => c.Post)
                .Include(c => c.User)
                .FirstOrDefaultAsync(m => m.CommentId == id);
            if (comment == null)
            {
                return NotFound();
            }

            return View(comment);
        }

        // POST: Comments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var comment = await _context.Comments.FindAsync(id);
            if (comment != null)
            {
                _context.Comments.Remove(comment);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CommentExists(int id)
        {
            return _context.Comments.Any(e => e.CommentId == id);
        }
    }
}
