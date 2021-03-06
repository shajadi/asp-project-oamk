﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using asp_project_oamk.Models;
using System.Data.SqlClient;
using System.Configuration;

namespace asp_project_oamk.Controllers
{
    public class MoviesController : Controller
    {
        private IMDBContext db = new IMDBContext();

        public ActionResult Index()
        {
            
            var movielst = db.Movies
                    .Include("Actors").ToList();
            
            foreach (var m in movielst )
            {
                var producer = db.Producers.Find(m.ProducerId);
                m.Producer = producer;
            }
            return View(movielst);
        }

        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Movie movie = db.Movies.Find(id);
            if (movie == null)
            {
                return HttpNotFound();
            }
            return View(movie);
        }

        public ActionResult Create()
        {

            MovieViewModel movievm = new MovieViewModel();


            var actor= db.Actors.ToList();

            string str = "";

            foreach (var sc in actor)
            {
                str = str + sc.ActorId.ToString() + ',';
            }

            Movie newmv = new Movie();
  
            movievm.Items = db.Actors.Select(x => new SelectListItem
            {
                Value = x.ActorId.ToString(),
                Text = x.Name
            });
            movievm.Producers = db.Producers.ToList();
            

            return View(movievm);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "MovieId,Name,Yor,Poster")] Movie movie, int ProducerId, string [] SelectedActorIds)
        {

            var idforimagename = 1; 
            try
            {
                try
                {
                    var lst = db.Movies;
                    if (lst.Any())
                    {
                        idforimagename = lst.Max(t => t.MovieId);
                        idforimagename++;
                    }
                }
                catch (Exception e) { }
                var poster = Request.Files["Poster"];
                if (poster.ContentLength >= 1048576)
                {
                    ViewBag.BrochureError = "Error : Image size should not exceed 1MB !";
                    return View(movie);
                }
                var str = poster.FileName.Substring(poster.FileName.LastIndexOf('.'));
                poster.SaveAs(Server.MapPath("~/") + @"Images/Movies/" + idforimagename + str);
                movie.Poster = "../../Images/Movies/" + idforimagename + str;
                movie.ProducerId = ProducerId; 
            }
            catch (Exception e)
            {

            }

            if (ModelState.IsValid)
            {
                db.Movies.Add(movie);
                db.SaveChanges();

                int mvid = movie.MovieId; 
                List<int> selectedactor = new List<int>();

                foreach (var sc in SelectedActorIds)
                {
                    selectedactor.Add(Int32.Parse(sc));
                }

                AddSeletedActors(selectedactor.ToArray(), movie.MovieId);

                return RedirectToAction("Index");
            }

            return View(movie);
        }

        public ActionResult Edit(int? id)
        {

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Movie movie = db.Movies.Find(id);
            if (movie == null)
            {
                return HttpNotFound();
            }


            MovieViewModel movievm = new MovieViewModel();


            var existingactor = db.Database.SqlQuery<QueryResult>("SELECT * FROM ActorMovie").Where(x=> x.intMovieid == movie.MovieId).ToList();
            string str = "";

            foreach (var sc in existingactor)
            {
                str = str + sc.intActorId.ToString() + ',';
            }

            movievm.SelectedActorIds = str.Split(',');
           
            movievm.Items = db.Actors.Select(x => new SelectListItem
            {
                Value = x.ActorId.ToString(),
                Text = x.Name
            });
            movievm.Producers = db.Producers.ToList();

            movievm.MovieId = movie.MovieId;
            movievm.Name = movie.Name;
            movievm.Yor = movie.Yor;
            movievm.Poster = movie.Poster;
            movievm.ProducerId = movie.ProducerId;
   
            return View(movievm);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "MovieId,Name,Yor,Poster")] Movie movie , int ProducerId, string[] SelectedActorIds)
        {



            var existingactor = db.Database.SqlQuery<QueryResult>("SELECT * FROM ActorMovie").Where(x => x.intMovieid == movie.MovieId).ToList();
            List<int> viewselectedactor = new List<int>();

            foreach (var sc in SelectedActorIds)
            {
                viewselectedactor.Add(Int32.Parse(sc));
            }

            int[] viewselectedlist1 = viewselectedactor.ToArray();

            int[] lstTableItems = existingactor.Select(x => x.intActorId).ToArray();

            int[] addactorLst = viewselectedlist1.Except(lstTableItems).ToArray();
            int[] removeactorLst = lstTableItems.Except(viewselectedlist1.Except(addactorLst)).ToArray();

            AddSeletedActors(addactorLst, movie.MovieId);

            RemoveDeselectedActors(removeactorLst, movie.MovieId);

            if (movie.Poster == null)
            {
                var mv = db.Movies.Find(movie.MovieId);
                db.Entry(mv).State = EntityState.Detached;
                movie.Poster = mv.Poster;
            }
            else
            {
                var idforimagename = 1; 
                try
                {
                    try
                    {
                        idforimagename = movie.MovieId;
                    }
                    catch (Exception e) { }
                    var poster = Request.Files["Poster"];
                    if (poster.ContentLength >= 1048576) { ViewBag.BrochureError = "Error : Image size should not exceed 1MB !"; return View(poster); }
                    var imageExtesnion = poster.FileName.Substring(poster.FileName.LastIndexOf('.'));
                    poster.SaveAs(Server.MapPath("~/") + @"Images/Movies/" + idforimagename + imageExtesnion);
                    movie.Poster = "../../Images/Movies/" + idforimagename + imageExtesnion;
                }
                catch (Exception)
                {

                }

            }

            if (ModelState.IsValid)
            {
                var mv = db.Movies.Find(movie.MovieId);
                db.Entry(mv).State = EntityState.Detached;
                movie.Actors = mv.Actors;
                movie.ProducerId = ProducerId;

                db.Entry(movie).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(movie);
        }



        private void AddSeletedActors(int[] actors, int movieid)
        {
            foreach (int actorid in actors)
            {
                String constr = ConfigurationManager.ConnectionStrings["IMDBContext"].ConnectionString;
                using (SqlConnection con = new SqlConnection(constr))
                {
                    try
                    {
                        con.Open();
                        try
                        {
                            using (SqlCommand cmd = new SqlCommand("INSERT INTO [dbo].[ActorMovie] (intActorId, intMovieId) VALUES (@intActorId, @intMovieId)", con))
                            {
                                cmd.Parameters.AddWithValue("@intActorId", actorid);
                                cmd.Parameters.AddWithValue("@intMovieId", movieid);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        catch(Exception e)
                        {

                        }
                    }
                    catch
                    {

                    }
                    finally
                    {
                        con.Close();
                    }


                }

            }

        }

        private void RemoveDeselectedActors(int[] actors , int movieid)
        {

            foreach(int actorid in actors )
            {
                String constr = ConfigurationManager.ConnectionStrings["IMDBContext"].ConnectionString;
                using (SqlConnection con = new SqlConnection(constr))
                {
                    try
                    {
                        con.Open();
                        try
                        {
                            using (SqlCommand cmd = new SqlCommand("DELETE FROM [dbo].[ActorMovie] where intActorId=@intActorId AND intMovieId=@intMovieId", con))
                            {
                                cmd.Parameters.AddWithValue("@intActorId", actorid);
                                cmd.Parameters.AddWithValue("@intMovieId", movieid);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        catch
                        {

                        }
                    }
                    catch
                    {

                    }
                    finally
                    {
                        con.Close();
                    }


                }

            }
            
        }

        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Movie movie = db.Movies.Find(id);
            if (movie == null)
            {
                return HttpNotFound();
            }
            return View(movie);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Movie movie = db.Movies.Find(id);

            
            String imgpath = movie.Poster.Substring(5);
            String fullpath = Server.MapPath("~/") + imgpath;
            if (System.IO.File.Exists(fullpath))
            {

                try
                {
                    System.IO.File.Delete(fullpath);
                }
                catch (System.IO.IOException e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            db.Movies.Remove(movie);
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
