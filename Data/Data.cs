using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.Linq;

namespace Data
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }

        public List<Likes> Likes { get; set; }
        public List<Answer> Answers { get; set; }
    }

    public class Question
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Text { get; set; }
        public DateTime DatePosted { get; set; }

        public List<QuestionsTags> QuestionsTags { get; set; }
        public List<Likes> Likes { get; set; }
        public List<Answer> Answers { get; set; }
    }

    public class Tag
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public List<QuestionsTags> QuestionsTags { get; set; }
    }

    public class QuestionsTags
    {
        public int QuestionId { get; set; }
        public int TagId { get; set; }
        public Question Question { get; set; }
        public Tag Tag { get; set; }
    }

    public class Answer
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public int QuestionId { get; set; }
        public int UserId { get; set; }
        public Question Question { get; set; }
        public User User { get; set; }
    }

    public class Likes
    {
        public int UserId { get; set; }
        public int QuestionId { get; set; }
        public User User { get; set; }
        public Question Question { get; set; }
    }

    public class QuestionsTagsContext : DbContext
    {
        private string _conn;
        public QuestionsTagsContext(string conn)
        {
            _conn = conn;
        }

        public DbSet<Question> Questions { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<QuestionsTags> QuestionsTags { get; set; }

        public DbSet<User> Users { get; set; }
        public DbSet<Answer> Answers { get; set; }
        public DbSet<Likes> Likes { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(_conn);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //Taken from here:
            //https://www.learnentityframeworkcore.com/configuration/many-to-many-relationship-configuration

            foreach (var relationship in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            {
                relationship.DeleteBehavior = DeleteBehavior.Restrict;
            }

            //set up composite primary key
            modelBuilder.Entity<QuestionsTags>()
                .HasKey(qt => new { qt.QuestionId, qt.TagId });
            modelBuilder.Entity<Likes>()
                .HasKey(l => new { l.QuestionId, l.UserId });

            //set up foreign key from QuestionsTags to Questions
            modelBuilder.Entity<QuestionsTags>()
                .HasOne(qt => qt.Question)
                .WithMany(q => q.QuestionsTags)
                .HasForeignKey(q => q.QuestionId);
            modelBuilder.Entity<Likes>()
                .HasOne(l => l.Question)
                .WithMany(q => q.Likes)
                .HasForeignKey(q => q.QuestionId);

            //set up foreign key from QuestionsTags to Tags
            modelBuilder.Entity<QuestionsTags>()
                .HasOne(qt => qt.Tag)
                .WithMany(t => t.QuestionsTags)
                .HasForeignKey(q => q.TagId);
            modelBuilder.Entity<Likes>()
                .HasOne(l => l.User)
                .WithMany(u => u.Likes)
                .HasForeignKey(u => u.UserId);
        }
    }

    public class QuestionsTagsContextFactory : IDesignTimeDbContextFactory<QuestionsTagsContext>
    {
        public QuestionsTagsContext CreateDbContext(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), $"..{Path.DirectorySeparatorChar}hmwk for 5.6"))
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true).Build();

            return new QuestionsTagsContext(config.GetConnectionString("ConStr"));
        }
    }

    public class QuestionsRepository
    {
        private string _conn;
        public QuestionsRepository(string conn)
        {
            _conn = conn;
        }

        public void AddUser(User u)
        {
            using (var context = new QuestionsTagsContext(_conn))
            {
                var user = new User
                {
                    Name = u.Name,
                    Email = u.Email,
                    Password = HashPassword(u.Password)
                };
                context.Users.Add(user);
                context.SaveChanges();
            }
        }

        public User GetUser(User user)
        {
            using (var context = new QuestionsTagsContext(_conn))
            {
                return context.Users.FirstOrDefault(u => u.Email == user.Email);
            }
        }
        public User GetUserByEmail(string email)
        {
            using (var context = new QuestionsTagsContext(_conn))
            {
                var user= context.Users.FirstOrDefault(u => u.Email == email);
                return user;
            }
        }

        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public bool Match(string input, string passwordHash)
        {
            return BCrypt.Net.BCrypt.Verify(input, passwordHash);
        }

        public bool DidntLikeYet(int questionId, int userId)
        {
            using (var context = new QuestionsTagsContext(_conn))
            {
                var question = context.Questions.Include(l=>l.Likes).FirstOrDefault(q => q.Id == questionId);
                return !question.Likes.Any(l => l.UserId == userId);
            }
        }

        private Tag GetTag(string name)
        {
            using (var context = new QuestionsTagsContext(_conn))
            {
                return context.Tags.FirstOrDefault(t => t.Name == name);
            }
        }

        private int AddTag(string name)
        {
            using (var context = new QuestionsTagsContext(_conn))
            {
                var tag = new Tag { Name = name };
                context.Tags.Add(tag);
                context.SaveChanges();
                return tag.Id;
            }
        }

        public void AddQuestion(Question q, IEnumerable<string> tags)
        {
            using (var context = new QuestionsTagsContext(_conn))
            {
                context.Questions.Add(q);
                foreach (string tag in tags)
                {
                    Tag t = GetTag(tag);
                    int tagId;
                    if (t == null)
                    {
                        tagId = AddTag(tag);
                    }
                    else
                    {
                        tagId = t.Id;
                    }
                    context.QuestionsTags.Add(new QuestionsTags
                    {
                        QuestionId = q.Id,
                        TagId = tagId
                    });
                }
                context.SaveChanges();
            }
        }

        public void AddLike(int questionId, User u)
        {
            using (var context = new QuestionsTagsContext(_conn))
            {
                var user = GetUser(u);
                Likes like = new Likes
                {
                    UserId = user.Id,
                    //User = user,
                    QuestionId = questionId,
                    //Question = context.Questions.FirstOrDefault(q => q.Id == questionId)
                };
                context.Likes.Add(like);

                context.SaveChanges();
            }
        }

        public void AddAnswer(Answer answer)
        {
            using (var context = new QuestionsTagsContext(_conn))
            {
                context.Answers.Add(answer);
                context.SaveChanges();
            }
        }

        public IEnumerable<Question> GetQuestions()
        {
            using (var context = new QuestionsTagsContext(_conn))
            {
                return context.Questions.OrderByDescending(q => q.DatePosted).ToList();
            }
        }

        public Question GetQuestionById(int id)
        {
            using (var context = new QuestionsTagsContext(_conn))
            {
                return context.Questions.Include(t=>t.QuestionsTags).Include(l=>l.Likes).Include(a=>a.Answers).FirstOrDefault(q => q.Id == id);
            }
        }

        public IEnumerable<Tag> GetTagsByQuestionId(int id)
        {
            using (var context = new QuestionsTagsContext(_conn))
            {
                List<Tag> tags = new List<Tag>();
                List<QuestionsTags> qsts = context.Questions.Include(q => q.QuestionsTags).FirstOrDefault(q => q.Id == id).QuestionsTags.ToList();
                foreach (QuestionsTags qt in qsts)
                {
                    tags.Add(qt.Tag);
                }
                return tags;
            }
        }

        public IEnumerable<Tag> GetTagsByQuestion(IEnumerable<QuestionsTags> ids)
        {
            using (var context = new QuestionsTagsContext(_conn))
            {
                List<Tag> tags = new List<Tag>();
                foreach (QuestionsTags qt in ids)
                {
                    tags.Add(context.Tags.FirstOrDefault(t=>t.Id==qt.TagId));
                }
                return tags;
            }
        }

        public int GetLikesByQuestionId(int id)
        {
            using (var context = new QuestionsTagsContext(_conn))
            {
                return context.Questions.FirstOrDefault(q => q.Id == id).Likes.Count;
            }
        }

        public IEnumerable<Answer> GetAnswersByQuestionId(int id)
        {
            using (var context = new QuestionsTagsContext(_conn))
            {
                return context.Answers.Where(a => a.QuestionId == id).ToList();
            }
        }
    }

}
