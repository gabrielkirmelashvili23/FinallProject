using FinallProj.Exceptions;
using FinallProj.Helpers;
using FinallProj.Models;
using FinallProj.Models.Enums;
using static FinallProj.Helpers.Translator;

namespace FinallProj.Managers
{
    internal class AssignmentManager
    {
        public const int LateDays = 7;
        public const long MaxFileBytes = 5 * 1024 * 1024;

        private List<Assignment> _assignments;
        private List<Submission> _submissions;
        private AcademicManager _academic;

        public AssignmentManager(List<Assignment> assignments, List<Submission> submissions, AcademicManager academic)
        {
            _assignments = assignments;
            _submissions = submissions;
            _academic = academic;
        }

        public List<Assignment> GetForGroups(List<int> groupIds)
        {
            return _assignments
                .Where(item => groupIds.Contains(item.GroupId))
                .OrderBy(item => item.Deadline)
                .ThenBy(item => item.Id)
                .ToList();
        }

        public List<Assignment> GetAll()
        {
            return _assignments.ToList();
        }

        public List<Submission> GetAllSubmissions()
        {
            return _submissions.ToList();
        }

        public Assignment Find(int id)
        {
            Assignment? assignment = _assignments.FirstOrDefault(item => item.Id == id);

            if (assignment == null)
                throw new NotFoundException(T("Assignment not found!"));

            return assignment;
        }

        public Assignment Add(User user, int groupId, string title, string description, DateTime deadline, int maxPoints)
        {
            if (!Access.CanManageGroup(user, groupId))
                throw new ValidationException(T("You can only manage assignments of your own groups!"));

            if (title.Trim().Length < 3)
                throw new ValidationException(T("The title must have at least 3 characters."));

            if (description.Trim().Length < 5)
                throw new ValidationException(T("The description must have at least 5 characters."));

            if (deadline.Date < Clock.Today)
                throw new ValidationException(T("The deadline cannot be in the past!"));

            if (maxPoints < 1 || maxPoints > AcademicManager.MaxEntryPoints)
                throw new ValidationException(T("The maximum points must be between {0} and {1}.", 1, AcademicManager.MaxEntryPoints));

            Assignment assignment = new Assignment
            {
                Id = _assignments.Count == 0 ? 1 : _assignments.Max(item => item.Id) + 1,
                GroupId = groupId,
                LecturerUsername = user.Username,
                Title = DataStore.Clean(title),
                Description = DataStore.Clean(description),
                Created = Clock.Today,
                Deadline = deadline.Date,
                MaxPoints = maxPoints
            };

            _assignments.Add(assignment);

            return assignment;
        }

        public Assignment Delete(User user, int assignmentId)
        {
            Assignment assignment = Find(assignmentId);

            if (!Access.CanManageGroup(user, assignment.GroupId))
                throw new ValidationException(T("You can only manage assignments of your own groups!"));

            RemoveAssignment(assignment);

            return assignment;
        }

        public void RemoveOfGroup(int groupId)
        {
            foreach (Assignment assignment in _assignments.Where(item => item.GroupId == groupId).ToList())
            {
                RemoveAssignment(assignment);
            }
        }

        public void RemoveOfStudent(int studentId)
        {
            List<int> ids = _submissions.Where(item => item.StudentId == studentId).Select(item => item.Id).ToList();

            _academic.RemoveSubmissionScores(ids);
            _submissions.RemoveAll(item => item.StudentId == studentId);
        }

        private void RemoveAssignment(Assignment assignment)
        {
            List<int> ids = _submissions.Where(item => item.AssignmentId == assignment.Id).Select(item => item.Id).ToList();

            _academic.RemoveSubmissionScores(ids);
            _submissions.RemoveAll(item => item.AssignmentId == assignment.Id);
            _assignments.Remove(assignment);
        }

        public Submission? GetSubmission(int assignmentId, int studentId)
        {
            return _submissions.FirstOrDefault(item => item.AssignmentId == assignmentId && item.StudentId == studentId);
        }

        public List<Submission> GetSubmissions(int assignmentId)
        {
            return _submissions.Where(item => item.AssignmentId == assignmentId).OrderBy(item => item.Date).ThenBy(item => item.Id).ToList();
        }

        public Submission Submit(Student student, Assignment assignment, SubmissionKind kind, string content)
        {
            if (student.GroupId != assignment.GroupId)
                throw new ValidationException(T("This assignment is not for your group!"));

            DateTime today = Clock.Today;

            if (today > assignment.Deadline.AddDays(LateDays))
                throw new ValidationException(T("Submissions are closed: the deadline passed more than {0} days ago!", LateDays));

            Submission? existing = GetSubmission(assignment.Id, student.Id);

            if (existing != null && existing.IsReviewed)
                throw new ValidationException(T("This work is already reviewed and cannot be changed!"));

            content = content.Trim();

            if (kind == SubmissionKind.Text && content.Length < 3)
                throw new ValidationException(T("The answer must have at least 3 characters."));

            if (kind == SubmissionKind.Link && !(content.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || content.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
                throw new ValidationException(T("A link must start with http:// or https://"));

            if (kind == SubmissionKind.File)
            {
                content = StoreFile(content, assignment.Id, student);
            }

            if (existing == null)
            {
                existing = new Submission
                {
                    Id = _submissions.Count == 0 ? 1 : _submissions.Max(item => item.Id) + 1,
                    AssignmentId = assignment.Id,
                    StudentId = student.Id
                };

                _submissions.Add(existing);
            }

            existing.Date = today;
            existing.Kind = kind;
            existing.Content = DataStore.Clean(content);
            existing.IsLate = today > assignment.Deadline;

            return existing;
        }

        private string StoreFile(string path, int assignmentId, Student student)
        {
            path = path.Trim('"', ' ');

            if (!File.Exists(path))
                throw new ValidationException(T("The file was not found!"));

            FileInfo info = new FileInfo(path);

            if (info.Length > MaxFileBytes)
                throw new ValidationException(T("The file is too large (max {0} MB)!", MaxFileBytes / 1024 / 1024));

            string folder = Path.Combine(App.Files.DataFolder, "Uploads", $"assignment_{assignmentId}");
            Directory.CreateDirectory(folder);

            string target = Path.Combine(folder, $"{student.RollNumber}_{DataStore.Clean(info.Name).Replace('/', '_')}");
            File.Copy(path, target, true);

            return target.Replace('\\', '/');
        }

        public Submission Review(User user, Submission submission, int score, string comment)
        {
            Assignment assignment = Find(submission.AssignmentId);

            if (!Access.CanManageGroup(user, assignment.GroupId))
                throw new ValidationException(T("You can only manage assignments of your own groups!"));

            if (score < 0 || score > assignment.MaxPoints)
                throw new ValidationException(T("The score must be between {0} and {1}.", 0, assignment.MaxPoints));

            if (comment.Trim().Length < 2)
                throw new ValidationException(T("A comment is required."));

            Student student = App.StudentManager.GetAllStudents().First(item => item.Id == submission.StudentId);

            submission.IsReviewed = true;
            submission.Score = score;
            submission.Comment = DataStore.Clean(comment);
            submission.ReviewedBy = user.Username;
            submission.ReviewedDate = Clock.Today;

            _academic.ReplaceSubmissionScore(user, student, submission, score, T("Assignment: {0}", assignment.Title));

            return submission;
        }
    }
}
