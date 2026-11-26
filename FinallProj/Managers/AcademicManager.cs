using FinallProj.Exceptions;
using FinallProj.Helpers;
using FinallProj.Models;
using FinallProj.Models.Enums;
using static FinallProj.Helpers.Translator;

namespace FinallProj.Managers
{
    internal class AcademicManager
    {
        public const int ActivityTarget = 100;
        public const double ActivityWeight = 0.4;
        public const double ExamWeight = 0.6;
        public const double FinalMultiplier = 2;
        public const double PassGpa = 2.5;
        public const int MaxEntryPoints = 100;

        private List<ScoreEntry> _scores;
        private List<YearRecord> _records;
        private ExamManager _exams;

        public AcademicManager(List<ScoreEntry> scores, List<YearRecord> records, ExamManager exams)
        {
            _scores = scores;
            _records = records;
            _exams = exams;
        }

        public ScoreEntry AddScore(User user, Student student, int points, string reason, DateTime date, int lectureId = 0, int submissionId = 0)
        {
            if (user.Role == UserRole.Student)
                throw new ValidationException(T("Only lecturers and the admin can give points!"));

            if (student.GroupId == 0)
                throw new ValidationException(T("The student is not enrolled in a group!"));

            if (!Access.CanManageGroup(user, student.GroupId))
                throw new ValidationException(T("You can only give points to students of your own groups!"));

            if (points == 0 || points < -MaxEntryPoints || points > MaxEntryPoints)
                throw new ValidationException(T("The points must be between {0} and {1} and not zero.", -MaxEntryPoints, MaxEntryPoints));

            if (date.Date > Clock.Today)
                throw new ValidationException(T("Points cannot be recorded for a future date!"));

            ScoreEntry entry = new ScoreEntry
            {
                Id = _scores.Count == 0 ? 1 : _scores.Max(item => item.Id) + 1,
                StudentId = student.Id,
                LecturerUsername = user.Username,
                Date = date.Date,
                Points = points,
                Reason = reason.Trim().Length == 0 ? T("Lesson activity") : DataStore.Clean(reason),
                LectureId = lectureId,
                SubmissionId = submissionId
            };

            _scores.Add(entry);

            return entry;
        }

        public void ReplaceSubmissionScore(User user, Student student, Submission submission, int points, string reason)
        {
            _scores.RemoveAll(item => item.SubmissionId == submission.Id);

            if (points == 0)
            {
                return;
            }

            ScoreEntry entry = AddScore(user, student, points, reason, Clock.Today, 0, submission.Id);
            entry.Date = Clock.Today;
        }

        public void RemoveSubmissionScores(List<int> submissionIds)
        {
            _scores.RemoveAll(item => item.SubmissionId != 0 && submissionIds.Contains(item.SubmissionId));
        }

        public ScoreEntry DeleteScore(User user, int scoreId)
        {
            ScoreEntry? entry = _scores.FirstOrDefault(item => item.Id == scoreId);

            if (entry == null)
                throw new NotFoundException(T("Entry not found!"));

            if (entry.SubmissionId != 0)
                throw new ValidationException(T("This entry comes from an assignment. Change the review of the assignment instead!"));

            if (user.Role != UserRole.Admin && entry.LecturerUsername != user.Username)
                throw new ValidationException(T("You can only delete your own entries!"));

            _scores.Remove(entry);

            return entry;
        }

        public List<ScoreEntry> GetScores(int studentId)
        {
            return _scores
                .Where(item => item.StudentId == studentId)
                .OrderByDescending(item => item.Date)
                .ThenByDescending(item => item.Id)
                .ToList();
        }

        public int Points(int studentId, Semester? semester)
        {
            return _scores
                .Where(item => item.StudentId == studentId && (semester == null || semester.Contains(item.Date)))
                .Sum(item => item.Points);
        }

        public int CountEntries(int studentId, Semester? semester)
        {
            return _scores.Count(item => item.StudentId == studentId && (semester == null || semester.Contains(item.Date)));
        }

        public void RemoveOfStudent(int studentId)
        {
            _scores.RemoveAll(item => item.StudentId == studentId);
            _records.RemoveAll(item => item.StudentId == studentId);
        }

        public List<RankRow> Rating(List<Student> students, Semester? semester, bool descending)
        {
            List<RankRow> rows = students
                .Select(student => new RankRow
                {
                    Student = student,
                    Points = Points(student.Id, semester),
                    Entries = CountEntries(student.Id, semester)
                })
                .OrderByDescending(row => row.Points)
                .ThenBy(row => row.Student.Name)
                .ToList();

            for (int i = 0; i < rows.Count; i++)
            {
                if (i > 0 && rows[i].Points == rows[i - 1].Points)
                {
                    rows[i].Position = rows[i - 1].Position;
                }
                else
                {
                    rows[i].Position = i + 1;
                }
            }

            if (!descending)
            {
                rows.Reverse();
            }

            return rows;
        }

        public static char LetterOf(double percent)
        {
            if (percent >= 90) return 'A';
            if (percent >= 80) return 'B';
            if (percent >= 70) return 'C';
            if (percent >= 60) return 'D';
            if (percent >= 50) return 'E';

            return 'F';
        }

        public List<Semester> GetSemesters(Student student)
        {
            List<DateTime> dates = new List<DateTime>();

            Group? group = App.CourseManager.GetGroupOfStudent(student);

            if (group != null && group.StartDate.Date <= Clock.Today)
            {
                dates.Add(group.StartDate);
            }

            dates.AddRange(_scores.Where(item => item.StudentId == student.Id).Select(item => item.Date));
            dates.AddRange(_exams.GetAllResults().Where(item => item.StudentId == student.Id).Select(item => item.Date));

            if (dates.Count == 0)
            {
                return new List<Semester>();
            }

            return Semester.Range(dates.Min(), Clock.Today);
        }

        public SemesterResult CalculateSemester(Student student, Semester semester)
        {
            SemesterResult result = new SemesterResult { Semester = semester };

            result.ActivityPoints = Points(student.Id, semester);
            result.ActivityPercent = Math.Clamp(result.ActivityPoints * 100.0 / ActivityTarget, 0, 100);

            double weighted = 0;
            double weights = 0;

            foreach (Exam exam in _exams.RootsInSemester(student.GroupId, semester))
            {
                int attempts = _exams.AttemptsOf(exam.Id, student.Id).Count;
                int best = _exams.BestPoints(exam.Id, student.Id);

                ExamLine line = new ExamLine
                {
                    Exam = exam,
                    Attempts = attempts,
                    BestPoints = best,
                    Percent = attempts == 0 ? 0 : best * 100.0 / exam.MaxPoints,
                    Passed = _exams.HasPassed(exam.Id, student.Id)
                };

                result.Exams.Add(line);

                if (attempts > 0)
                {
                    double weight = exam.IsFinal ? FinalMultiplier : 1;
                    weighted += line.Percent * weight;
                    weights += weight;
                }
            }

            if (weights > 0)
            {
                result.ExamPercent = weighted / weights;
                result.TotalPercent = result.ExamPercent.Value * ExamWeight + result.ActivityPercent * ActivityWeight;
            }
            else
            {
                result.TotalPercent = result.ActivityPercent;
            }

            result.Gpa = Math.Round(result.TotalPercent / 20.0, 2);
            result.Letter = LetterOf(result.TotalPercent);
            result.FinalStatus = _exams.FinalStatusOf(student, semester);
            result.Closed = semester.End < Clock.Today;
            result.Provisional = !result.Closed || result.ExamPercent == null;

            return result;
        }

        public List<SemesterResult> GetSemesterResults(Student student)
        {
            return GetSemesters(student).Select(semester => CalculateSemester(student, semester)).ToList();
        }

        public List<YearResult> GetYears(Student student)
        {
            List<SemesterResult> all = GetSemesterResults(student);
            List<YearResult> years = new List<YearResult>();

            foreach (IGrouping<int, SemesterResult> group in all.GroupBy(item => item.Semester.StartYear).OrderBy(item => item.Key))
            {
                YearResult year = new YearResult
                {
                    StartYear = group.Key,
                    Semesters = group.OrderBy(item => item.Semester.Number).ToList()
                };

                year.Gpa = Math.Round(year.Semesters.Average(item => item.Gpa), 2);
                year.Letter = LetterOf(year.Gpa * 20);
                year.Outcome = OutcomeOf(year);
                year.IsClosed = _records.Any(item => item.StartYear == year.StartYear && item.StudentId == student.Id);

                years.Add(year);
            }

            return years;
        }

        public double CumulativeGpa(Student student)
        {
            List<SemesterResult> all = GetSemesterResults(student);

            return all.Count == 0 ? 0 : Math.Round(all.Average(item => item.Gpa), 2);
        }

        public static DateTime YearEnd(int startYear)
        {
            return new DateTime(startYear + 1, 8, 31);
        }

        private YearOutcome OutcomeOf(YearResult year)
        {
            if (YearEnd(year.StartYear) >= Clock.Today)
            {
                return YearOutcome.InProgress;
            }

            if (year.Semesters.Any(item => item.FinalStatus == FinalStatus.MustRepeat))
            {
                return YearOutcome.MustRepeat;
            }

            if (year.Semesters.Any(item => item.FinalStatus == FinalStatus.Stuck || item.FinalStatus == FinalStatus.Pending))
            {
                return YearOutcome.StuckInFinal;
            }

            return year.Gpa < PassGpa ? YearOutcome.LowGpa : YearOutcome.Promoted;
        }

        public YearRecord? GetRecord(int startYear, int studentId)
        {
            return _records.FirstOrDefault(item => item.StartYear == startYear && item.StudentId == studentId);
        }

        public List<YearRecord> GetRecords(int startYear)
        {
            return _records.Where(item => item.StartYear == startYear).ToList();
        }

        public (int Created, int Updated, int Promoted) CloseYear(User user, int startYear, List<Student> students)
        {
            if (user.Role != UserRole.Admin)
                throw new ValidationException(T("Only the admin can close an academic year!"));

            if (YearEnd(startYear) >= Clock.Today)
                throw new ValidationException(T("The academic year {0}/{1} has not ended yet (it ends on {2})!", startYear, startYear + 1, Format.Date(YearEnd(startYear))));

            int created = 0;
            int updated = 0;
            int promoted = 0;

            foreach (Student student in students)
            {
                YearResult? year = GetYears(student).FirstOrDefault(item => item.StartYear == startYear);

                if (year == null)
                {
                    continue;
                }

                YearRecord? record = GetRecord(startYear, student.Id);

                if (record != null && !(record.Outcome != YearOutcome.Promoted && year.Outcome == YearOutcome.Promoted))
                {
                    continue;
                }

                if (record == null)
                {
                    record = new YearRecord { StartYear = startYear, StudentId = student.Id };
                    _records.Add(record);
                    created++;
                }
                else
                {
                    updated++;
                }

                record.Outcome = year.Outcome;
                record.Gpa = year.Gpa;
                record.Letter = year.Letter;
                record.ClosedOn = Clock.Today;

                if (year.Outcome == YearOutcome.Promoted)
                {
                    student.UpdateGrade(year.Letter);
                    promoted++;
                }
            }

            return (created, updated, promoted);
        }

        public List<YearRecord> AllRecords()
        {
            return _records.ToList();
        }
    }
}
