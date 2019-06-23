using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using hmwk_for_5._6.Models;
using Microsoft.Extensions.Configuration;
using Data;
using Microsoft.AspNetCore.Authorization;

namespace hmwk_for_5._6.Controllers
{
    public class HomeController : Controller
    {
        private string _conn;
        public HomeController(IConfiguration configuration)
        {
            _conn = configuration.GetConnectionString("ConStr");
        }

        public IActionResult Index()
        {
            var repository = new QuestionsRepository(_conn);
            return View(repository.GetQuestions());
        }

        public IActionResult QuestionPage(int id)
        {
            var repository = new QuestionsRepository(_conn);

            var vm = new QuestionPageViewModel();

            vm.Question = repository.GetQuestionById(id);
            vm.Tags = repository.GetTagsByQuestion(vm.Question.QuestionsTags);
            vm.Answers = repository.GetAnswersByQuestionId(id);
            vm.IsLoggedIn = User.Identity.IsAuthenticated;
            if (User.Identity.IsAuthenticated)
            {
                vm.User = repository.GetUserByEmail(User.Identity.Name);
                vm.DidntLikeYet = repository.DidntLikeYet(id, vm.User.Id);
            }
            

            return View(vm);
        }

        [Authorize]
        public IActionResult AskQuestion()
        {
            return View();
            //var repository = new QuestionsRepository(_conn);
            //if (User.Identity.IsAuthenticated)
            //{
            //    return View();
            //}
            //else
            //{
            //    return Redirect("/account/login");
            //}

        }

        [HttpPost]
        public IActionResult SubmitQuestion(Question q, IEnumerable<string> tags)
        {
            var repository = new QuestionsRepository(_conn);
            repository.AddQuestion(q, tags);
            return Redirect("/home/index");
        }

        [HttpPost]
        public IActionResult AddLike(int questionId)
        {
            var repository = new QuestionsRepository(_conn);
            var u=repository.GetUserByEmail(User.Identity.Name);
            repository.AddLike(questionId, u);
            return Redirect($"/home/questionpage?id={questionId}");

           
        }

        [HttpPost]
        public IActionResult AddAnswer(Answer answer)
        {
            var repository = new QuestionsRepository(_conn);
            repository.AddAnswer(answer);
            return Redirect($"/home/questionpage?id={answer.Id}");
        }

        
    }

    
}


