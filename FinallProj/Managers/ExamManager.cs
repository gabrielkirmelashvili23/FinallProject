using FinallProj.Exceptions;
using FinallProj.Helpers;
using FinallProj.Models;
using FinallProj.Models.Enums;
using static FinallProj.Helpers.Translator;

namespace FinallProj.Managers
{
    internal class ExamManager
    {
        public const int MaxAttempts = 3;
        public const int PassPercent = 50;

        private List<Exam> _exams;
        private List<ExamResult> _results;

        public ExamManager(List<Exam> exams, List<ExamResult> results)
        {
            _exams = exams;
            _results = results;
        }

        public List<Exam> GetAll()
        {
            return _exams.OrderBy(exam => exam.Date).ThenBy(exam => exam.StartTime).ToList();
        }

        public List<ExamResult> GetAllResults()
        {
            return _results.ToList();
        }

        public Exam Find(int id)
        {
            Exam? exam = _exams.FirstOrDefault(item => item.Id == id);

            if (exam == null)
                throw new NotFoundException(T("Exam not found!"));

            return exam;
        }

        public Exam Add(User user, int groupId, string title, DateTime date, string time, string place, string details, bool isFinal, int maxPoints)
        {
            if (!Access.CanManageGroup(user, groupId))
                throw new ValidationException(T("You can only manage exams of your own groups!"));

            if (date.Date < Clock.Today)
                throw new ValidationException(T("An exam cannot be planned in the past!"));

            Exam exam = Build(NextId(), groupId, title, date, time, place, details, isFinal, maxPoints, 0, user.Username);

            if (isFinal)
            {
                Semester semester = Semester.Of(date);

                bool exists = _exams.Any(item => item.RetakeOf == 0 && item.IsFinal && item.GroupId == groupId && semester.Contains(item.Date));

                if (exists)
                    throw new ValidationException(T("This group already has a final exam in the semester {0}!", semester.Label));
            }

            CheckSameTime(exam, 0);
            _exams.Add(exam);

            return exam;
        }

        public Exam AddRetake(User user, int rootId, DateTime date, string time, string place, string details)
        {
            Exam root = Find(rootId);

            if (root.RetakeOf != 0)
                throw new ValidationException(T("Choose the original exam, not a retake!"));

            if (!Access.CanManageGroup(user, root.GroupId))
                throw new ValidationException(T("You can only manage exams of your own groups!"));

            if (date.Date < Clock.Today)
                throw new ValidationException(T("An exam cannot be planned in the past!"));

            List<Student> waiting = StudentsNeedingRetake(root);

            if (waiting.Count == 0)
                throw new ValidationException(T("Nobody needs a retake of this exam!"));

            DateTime last = _exams.Where(item => item.RootId == root.Id).Max(item => item.Date);

            if (date.Date <= last.Date)
                throw new ValidationException(T("The retake must be later than the previous sitting ({0})!", Format.Date(last)));

            Exam retake = Build(NextId(), root.GroupId, root.Title, date, time, place, details, root.IsFinal, root.MaxPoints, root.Id, user.Username);
            CheckSameTime(retake, 0);
            _exams.Add(retake);

            return retake;
        }

        public Exam Reschedule(User user, int examId, DateTime date, string time, string place)
        {
            Exam exam = Find(examId);

            if (!Access.CanManageGroup(user, exam.GroupId))
                throw new ValidationException(T("You can only manage exams of your own groups!"));

            if (_results.Any(result => result.ExamId == exam.Id))
                throw new ValidationException(T("Results are already recorded, the exam cannot be moved!"));

            if (date.Date < Clock.Today)
                throw new ValidationException(T("An exam cannot be planned in the past!"));

            Exam changed = Build(exam.Id, exam.GroupId, exam.Title, date, time, place, exam.Details, exam.IsFinal, exam.MaxPoints, exam.RetakeOf, exam.CreatedBy);
            CheckSameTime(changed, exam.Id);

            exam.Date = changed.Date;
            exam.StartTime = changed.StartTime;
            exam.Place = changed.Place;

            return exam;
        }

        public Exam Delete(User user, int examId)
        {
            Exam exam = Find(examId);

            if (!Access.CanManageGroup(user, exam.GroupId))
                throw new ValidationException(T("You can only manage exams of your own groups!"));

            if (_results.Any(result => result.ExamId == exam.Id))
                throw new ValidationException(T("Results are already recorded, the exam cannot be deleted!"));

            if (_exams.Any(item => item.RetakeOf == exam.Id))
                throw new ValidationException(T("Delete the retakes of this exam first!"));

            _exams.Remove(exam);

            return exam;
        }

        public ExamResult RecordResult(User user, Exam exam, Student student, int points)
        {
            if (!Access.CanManageGroup(user, exam.GroupId))
                throw new ValidationException(T("You can only manage exams of your own groups!"));

            if (student.GroupId != exam.GroupId)
                throw new ValidationException(T("This student is not in the group of the exam!"));

            if (exam.Date.Date > Clock.Today)
                throw new ValidationException(T("The exam has not taken place yet ({0})!", Format.Date(exam.Date)));

            if (points < 0 || points > exam.MaxPoints)
                throw new ValidationException(T("The points must be between {0} and {1}.", 0, exam.MaxPoints));

            List<ExamResult> attempts = AttemptsOf(exam.RootId, student.Id);

            if (exam.RetakeOf == 0 && attempts.Count > 0)
                throw new ValidationException(T("This student already has a result. Schedule a retake instead!"));

            if (exam.RetakeOf != 0)
            {
                if (attempts.Count == 0)
                    throw new ValidationException(T("This student has not taken the original exam!"));

                if (HasPassed(exam.RootId, student.Id))
                    throw new ValidationException(T("This student has already passed this exam!"));

                if (attempts.Count >= MaxAttempts)
                    throw new ValidationException(T("The student used all {0} attempts!", MaxAttempts));

                if (attempts.Any(item => item.ExamId == exam.Id))
                    throw new ValidationException(T("The result for this sitting is already recorded!"));

                if (Clock.Today <= attempts.Max(item => item.Date))
                    throw new ValidationException(T("A retake must be recorded on a later day than the previous attempt!"));
            }

            ExamResult result = new ExamResult
            {
                Id = _results.Count == 0 ? 1 : _results.Max(item => item.Id) + 1,
                ExamId = exam.Id,
                StudentId = student.Id,
                Attempt = attempts.Count + 1,
                Points = points,
                Date = Clock.Today,
                RecordedBy = user.Username
            };

            _results.Add(result);

            return result;
        }

        public List<ExamResult> AttemptsOf(int rootId, int studentId)
        {
            List<int> sittings = _exams.Where(exam => exam.RootId == rootId).Select(exam => exam.Id).ToList();

            return _results
                .Where(result => result.StudentId == studentId && sittings.Contains(result.ExamId))
                .OrderBy(result => result.Attempt)
                .ToList();
        }

        public int BestPoints(int rootId, int studentId)
        {
            List<ExamResult> attempts = AttemptsOf(rootId, studentId);

            return attempts.Count == 0 ? 0 : attempts.Max(result => result.Points);
        }

        public bool HasPassed(int rootId, int studentId)
        {
            List<ExamResult> attempts = AttemptsOf(rootId, studentId);

            if (attempts.Count == 0)
            {
                return false;
            }

            Exam root = Find(rootId);

            return attempts.Max(result => result.Points) * 100.0 / root.MaxPoints >= PassPercent;
        }

        public bool NeedsRetake(Exam root, Student student)
        {
            List<ExamResult> attempts = AttemptsOf(root.Id, student.Id);

            return attempts.Count > 0 && attempts.Count < MaxAttempts && !HasPassed(root.Id, student.Id);
        }

        public List<Student> StudentsNeedingRetake(Exam root)
        {
            return App.StudentManager.GetAllStudents()
                .Where(student => student.GroupId == root.GroupId && NeedsRetake(root, student))
                .ToList();
        }

        public List<Student> StudentsWaitingFor(Exam exam)
        {
            List<Student> students = App.StudentManager.GetAllStudents().Where(student => student.GroupId == exam.GroupId).ToList();

            if (exam.RetakeOf == 0)
            {
                return students.Where(student => AttemptsOf(exam.Id, student.Id).Count == 0).ToList();
            }

            Exam root = Find(exam.RootId);

            return students
                .Where(student => NeedsRetake(root, student) && !AttemptsOf(root.Id, student.Id).Any(result => result.ExamId == exam.Id))
                .ToList();
        }

        public List<Exam> GetForGroups(List<int> groupIds, DateTime from)
        {
            return _exams
                .Where(exam => groupIds.Contains(exam.GroupId) && exam.Date.Date >= from.Date)
                .OrderBy(exam => exam.Date)
                .ThenBy(exam => exam.StartTime)
                .ToList();
        }

        public List<Exam> GetPast(List<int> groupIds, DateTime before)
        {
            return _exams
                .Where(exam => groupIds.Contains(exam.GroupId) && exam.Date.Date < before.Date)
                .OrderByDescending(exam => exam.Date)
                .ToList();
        }

        public FinalStatus FinalStatusOf(Student student, Semester semester)
        {
            Exam? final = _exams.FirstOrDefault(exam => exam.RetakeOf == 0 && exam.IsFinal && exam.GroupId == student.GroupId && semester.Contains(exam.Date));

            if (final == null)
            {
                return FinalStatus.NoFinal;
            }

            List<ExamResult> attempts = AttemptsOf(final.Id, student.Id);

            if (HasPassed(final.Id, student.Id))
            {
                return FinalStatus.Passed;
            }

            if (attempts.Count == 0)
            {
                return FinalStatus.Pending;
            }

            return attempts.Count >= MaxAttempts ? FinalStatus.MustRepeat : FinalStatus.Stuck;
        }

        public List<Exam> RootsInSemester(int groupId, Semester semester)
        {
            return _exams.Where(exam => exam.RetakeOf == 0 && exam.GroupId == groupId && semester.Contains(exam.Date)).OrderBy(exam => exam.Date).ToList();
        }

        public void RemoveOfGroup(int groupId)
        {
            List<int> ids = _exams.Where(exam => exam.GroupId == groupId).Select(exam => exam.Id).ToList();

            _results.RemoveAll(result => ids.Contains(result.ExamId));
            _exams.RemoveAll(exam => exam.GroupId == groupId);
        }

        public void RemoveOfStudent(int studentId)
        {
            _results.RemoveAll(result => result.StudentId == studentId);
        }

        private int NextId()
        {
            return _exams.Count == 0 ? 1 : _exams.Max(exam => exam.Id) + 1;
        }

        private Exam Build(int id, int groupId, string title, DateTime date, string time, string place, string details, bool isFinal, int maxPoints, int retakeOf, string createdBy)
        {
            if (title.Trim().Length < 3)
                throw new ValidationException(T("The title must have at least 3 characters."));

            if (!TimeText.TryParse(time, out TimeSpan start))
                throw new ValidationException(T("Use the time format HH:mm, for example 09:30."));

            if (place.Trim().Length < 2)
                throw new ValidationException(T("The room or link is required."));

            if (maxPoints < 10 || maxPoints > 1000)
                throw new ValidationException(T("The maximum points must be between {0} and {1}.", 10, 1000));

            return new Exam
            {
                Id = id,
                GroupId = groupId,
                Title = DataStore.Clean(title),
                Date = date.Date,
                StartTime = TimeText.Format(start),
                Place = DataStore.Clean(place),
                Details = DataStore.Clean(details),
                IsFinal = isFinal,
                MaxPoints = maxPoints,
                RetakeOf = retakeOf,
                CreatedBy = createdBy
            };
        }

        private void CheckSameTime(Exam exam, int ignoreId)
        {
            bool clash = _exams.Any(item => item.Id != ignoreId && item.GroupId == exam.GroupId && item.Date.Date == exam.Date.Date && item.StartTime == exam.StartTime);

            if (clash)
                throw new ValidationException(T("This group already has an exam at that time!"));
        }
    }
}
