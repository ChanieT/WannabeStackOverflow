using System;
using System.Collections.Generic;
using Data;

namespace hmwk_for_5._6.Models
{
    public class ErrorViewModel
    {
        public string RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }


    public class QuestionPageViewModel
    {
        public Question Question { get; set; }
        public IEnumerable<Tag> Tags { get; set; }
        public IEnumerable<Answer> Answers { get; set; }
        public bool IsLoggedIn { get; set; }
        public bool DidntLikeYet { get; set; }
        public User User { get; set; }
    }
}