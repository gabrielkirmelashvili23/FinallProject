using FinallProj.Exceptions;
using FinallProj.Helpers;
using FinallProj.Models;
using FinallProj.Models.Enums;
using static FinallProj.Helpers.Translator;

namespace FinallProj.Managers
{
    internal class LectureManager
    {
        private List<Lecture> _lectures;

        public LectureManager(List<Lecture> lectures)
        {
            _lectures = lectures;
        }

        public List<Lecture> GetAll()
        {
            return _lectures.OrderBy(lecture => lecture.Date).ThenBy(lecture => lecture.Start).ToList();
        }

        public Lecture Find(int id)
        {
            Lecture? lecture = _lectures.FirstOrDefault(item => item.Id == id);

            if (lecture == null)
                throw new NotFoundException(T("Lecture not found!"));

            return lecture;
        }

        private int NextId()
        {
            return _lectures.Count == 0 ? 1 : _lectures.Max(lecture => lecture.Id) + 1;
        }

        public Lecture Add(User user, int groupId, string lecturerUsername, DateTime date, string startTime, int minutes, string title, LectureFormat format, string place)
        {
            if (!Access.CanManageGroup(user, groupId))
                throw new ValidationException(T("You can only manage lectures of your own groups!"));

            if (date.Date < Clock.Today)
                throw new ValidationException(T("A lecture cannot be planned in the past!"));

            Lecture lecture = Build(NextId(), groupId, lecturerUsername, date, startTime, minutes, title, format, place);
            CheckConflicts(lecture, 0);

            _lectures.Add(lecture);

            return lecture;
        }

        public Lecture Reschedule(User user, int lectureId, DateTime date, string startTime, int minutes, string place)
        {
            Lecture old = Find(lectureId);
            CheckRights(user, old);

            if (old.Status == LectureStatus.Done)
                throw new ValidationException(T("A lecture that is already done cannot be moved!"));

            if (date.Date < Clock.Today)
                throw new ValidationException(T("A lecture cannot be planned in the past!"));

            Lecture changed = Build(old.Id, old.GroupId, old.LecturerUsername, date, startTime, minutes, old.Title, old.Format, place);
            changed.Status = LectureStatus.Planned;
            CheckConflicts(changed, old.Id);

            old.Date = changed.Date;
            old.StartTime = changed.StartTime;
            old.Minutes = changed.Minutes;
            old.Place = changed.Place;
            old.Status = LectureStatus.Planned;

            return old;
        }

        public Lecture SetStatus(User user, int lectureId, LectureStatus status)
        {
            Lecture lecture = Find(lectureId);
            CheckRights(user, lecture);

            if (status == LectureStatus.Done && lecture.Date.Date > Clock.Today)
                throw new ValidationException(T("A future lecture cannot be marked as done!"));

            lecture.Status = status;

            return lecture;
        }

        public Lecture Delete(User user, int lectureId)
        {
            Lecture lecture = Find(lectureId);
            CheckRights(user, lecture);

            _lectures.Remove(lecture);

            return lecture;
        }

        public int RemoveOfGroup(int groupId)
        {
            return _lectures.RemoveAll(lecture => lecture.GroupId == groupId);
        }

        public List<Lecture> GetForGroups(List<int> groupIds, DateTime from, DateTime to)
        {
            return _lectures
                .Where(lecture => groupIds.Contains(lecture.GroupId) && lecture.Date.Date >= from.Date && lecture.Date.Date <= to.Date)
                .OrderBy(lecture => lecture.Date)
                .ThenBy(lecture => lecture.Start)
                .ToList();
        }

        public List<Lecture> GetOfLecturer(string username, DateTime from, DateTime to)
        {
            return _lectures
                .Where(lecture => lecture.LecturerUsername == username && lecture.Date.Date >= from.Date && lecture.Date.Date <= to.Date)
                .OrderBy(lecture => lecture.Date)
                .ThenBy(lecture => lecture.Start)
                .ToList();
        }

        private void CheckRights(User user, Lecture lecture)
        {
            if (user.Role == UserRole.Admin)
            {
                return;
            }

            if (lecture.LecturerUsername == user.Username || Access.CanManageGroup(user, lecture.GroupId))
            {
                return;
            }

            throw new ValidationException(T("You can only manage your own lectures!"));
        }

        private Lecture Build(int id, int groupId, string lecturerUsername, DateTime date, string startTime, int minutes, string title, LectureFormat format, string place)
        {
            if (!TimeText.TryParse(startTime, out TimeSpan start))
                throw new ValidationException(T("Use the time format HH:mm, for example 09:30."));

            if (minutes < 15 || minutes > 300)
                throw new ValidationException(T("The duration must be between {0} and {1} minutes.", 15, 300));

            if (start.Add(TimeSpan.FromMinutes(minutes)) > TimeSpan.FromHours(24))
                throw new ValidationException(T("A lecture must finish before midnight!"));

            if (title.Trim().Length < 3)
                throw new ValidationException(T("The title must have at least 3 characters."));

            if (place.Trim().Length < 2)
                throw new ValidationException(T("The room or link is required."));

            if (format == LectureFormat.Online && !place.Trim().StartsWith("http", StringComparison.OrdinalIgnoreCase))
                throw new ValidationException(T("An online lecture needs a link starting with http."));

            return new Lecture
            {
                Id = id,
                GroupId = groupId,
                LecturerUsername = lecturerUsername,
                Date = date.Date,
                StartTime = TimeText.Format(start),
                Minutes = minutes,
                Title = DataStore.Clean(title),
                Format = format,
                Place = DataStore.Clean(place)
            };
        }

        private void CheckConflicts(Lecture lecture, int ignoreId)
        {
            foreach (Lecture other in _lectures)
            {
                if (other.Id == ignoreId || other.Status == LectureStatus.Cancelled || !lecture.Overlaps(other))
                {
                    continue;
                }

                if (other.GroupId == lecture.GroupId)
                    throw new ValidationException(T("This group already has a lecture at that time ({0}-{1})!", other.StartTime, other.EndTime));

                if (other.LecturerUsername == lecture.LecturerUsername && lecture.LecturerUsername != string.Empty)
                    throw new ValidationException(T("The lecturer is busy at that time ({0}-{1})!", other.StartTime, other.EndTime));

                if (lecture.Format == LectureFormat.Room && other.Format == LectureFormat.Room && string.Equals(other.Place, lecture.Place, StringComparison.OrdinalIgnoreCase))
                    throw new ValidationException(T("The room {0} is already taken at that time ({1}-{2})!", other.Place, other.StartTime, other.EndTime));
            }
        }
    }
}
