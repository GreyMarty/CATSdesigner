﻿using Application.Core;
using Application.Core.Helpers;
using Application.Infrastructure.LecturerManagement;
using Application.Infrastructure.Models;
using Application.Infrastructure.NoteManagement;
using Application.Infrastructure.PracticalManagement;
using Application.Infrastructure.ScheduleManagement;
using Application.Infrastructure.SubjectManagement;
using LMPlatform.Models;
using LMPlatform.UI.Attributes;
using LMPlatform.UI.Services.Modules;
using LMPlatform.UI.Services.Modules.Notes;
using LMPlatform.UI.Services.Modules.Schedule;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WebMatrix.WebData;

namespace LMPlatform.UI.Services.Schedule
{
    [JwtAuth]
    public class ScheduleService : IScheduleService
    {
        private readonly LazyDependency<IScheduleManagementService> scheduleManagementService = new LazyDependency<IScheduleManagementService>();
        private readonly LazyDependency<ISubjectManagementService> subjectManagementService = new LazyDependency<ISubjectManagementService>();
        private readonly LazyDependency<IPracticalManagementService> practicalManagementService = new LazyDependency<IPracticalManagementService>();
        private readonly LazyDependency<ILecturerManagementService>  lecturerManagementService = new LazyDependency<ILecturerManagementService>();
        public IScheduleManagementService ScheduleManagementService => scheduleManagementService.Value;
        public ISubjectManagementService SubjectManagementService => subjectManagementService.Value;
        public ILecturerManagementService LecturerManagementService => lecturerManagementService.Value;
        public IPracticalManagementService PracticalManagementService => practicalManagementService.Value;


        public ScheduleViewResult GetSchedule(string dateStart, string dateEnd)
        {
            var dateTimeStart = DateTime.ParseExact(dateStart, "dd-MM-yyyy", CultureInfo.InvariantCulture);
            var dateTimeEnd = DateTime.ParseExact(dateEnd, "dd-MM-yyyy", CultureInfo.InvariantCulture);
            var subjects = SubjectManagementService.GetUserSubjects(UserContext.CurrentUserId);
            return new ScheduleViewResult
            {
                Schedule = ScheduleManagementService.GetScheduleBetweenDates(dateTimeStart, dateTimeEnd)
                .Where(x => subjects.Any(s => s.Id == x.SubjectId))
                .Select(x => new ScheduleViewData(x))
            };
        }

        public ScheduleViewResult GetScheduleForDate(string date)
        {
            try
            {
                var dateTime = DateTime.ParseExact(date, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                return new ScheduleViewResult
                {
                    Schedule = ScheduleManagementService.GetScheduleForDate(dateTime).Select(x => new ScheduleViewData(x))
                };
            } catch(Exception ex)
            {
                return new ScheduleViewResult();
            }
        }

        public ScheduleViewResultSingle SaveDateLectures(int id, int subjectId, string date, string startTime, string endTime, string building, string audience, Note note, int? lecturerId)
        {

            try
            {
                var lecturesSchedule = SaveDateValidate(id, subjectId, date, startTime, endTime, building, audience,
                    note, lecturerId);
                var schedule = ScheduleManagementService.SaveDateLectures(new LecturesScheduleVisiting(lecturesSchedule)
                    { SubjectId = subjectId });
                return new ScheduleViewResultSingle
                {
                    Message = "Дата успешно добавлена",
                    Code = "200",
                    Schedule = new ScheduleViewData(
                        ScheduleManagementService.GetScheduleById(schedule.Id, ClassType.Lecture))

                };
            }
            catch (ScheduleBusyException ex)
            {
                var firstSchedule = ex.Schedule.First();
                return new ScheduleViewResultSingle
                {
                    Code = "500",
                    Message = "Время и место занято",
                    Lector = new LectorViewData(firstSchedule.Teacher, true),
                    GroupName = firstSchedule.GroupName,
                };
            }
            catch (Exception ex)
            {
                return new ScheduleViewResultSingle
                {
                    Message = ex is ArgumentException ? ex.Message : "Произошла ошибка при добавлении даты",
                    Code = "500"
                };
            }
        }

        public ScheduleViewResultSingle SaveDateLab(int id, int subjectId, int subGroupId, string date, string startTime, string endTime, string building, string audience, Note note, int? lecturerId)
		{
            try
			{
                var labsSchedule = SaveDateValidate(id, subjectId, date, startTime, endTime, building, audience, note, lecturerId);
                var schedule = ScheduleManagementService.SaveScheduleProtectionLabsDate(new ScheduleProtectionLabs(labsSchedule) { SuGroupId = subGroupId, SubjectId = subjectId });
				return new ScheduleViewResultSingle
				{
					Message = "Дата успешно добавлена",
					Code = "200",
                    Schedule = new ScheduleViewData(ScheduleManagementService.GetScheduleById(schedule.Id, ClassType.Lab))

                };
			}
            catch (ScheduleBusyException ex)
            {
                var firstSchedule = ex.Schedule.First();
                return new ScheduleViewResultSingle
                {
                    Code = "500",
                    Message = "Время и место занято",
                    Lector = new LectorViewData(firstSchedule.Teacher, true),
                    GroupName = firstSchedule.GroupName,
                };
            }
            catch (Exception ex)
            {
				return new ScheduleViewResultSingle
                {
					Message = ex is ArgumentException ? ex.Message : "Произошла ошибка при добавлении даты",
					Code = "500"
				};
			}
		}

        public ScheduleViewResultSingle SaveDatePractical(int id, int subjectId, int groupId, string date, string startTime, string endTime, string building, string audience, Note note, int? lecturerId)
        {
            try
            {
                var practicalsSchedule = SaveDateValidate(id, subjectId, date, startTime, endTime, building, audience, note, lecturerId);
                var schedule = ScheduleManagementService.SaveDatePractical(new ScheduleProtectionPractical(practicalsSchedule) { GroupId = groupId, SubjectId = subjectId });
                return new ScheduleViewResultSingle
                {
                    Message = "Дата успешно добавлена",
                    Code = "200",
                    Schedule = new ScheduleViewData(ScheduleManagementService.GetScheduleById(schedule.Id, ClassType.Practical))

                };
            }
            catch (ScheduleBusyException ex)
            {
                var firstSchedule = ex.Schedule.First();
                return new ScheduleViewResultSingle
                {
                    Code = "500",
                    Message = "Время и место занято",
                    Lector = new LectorViewData(firstSchedule.Teacher, true),
                    GroupName = firstSchedule.GroupName,
                };
            }
            catch (Exception ex)
            {
                return new ScheduleViewResultSingle
                {
                    Message = ex is ArgumentException ? ex.Message : "Произошла ошибка при добавлении даты",
                    Code = "500"
                };
            }
        }

        public ScheduleViewResult GetUserSchedule(string dateStart, string dateEnd)
        {
            var dateTimeStart = DateTime.ParseExact(dateStart, "dd-MM-yyyy", CultureInfo.InvariantCulture);
            var dateTimeEnd = DateTime.ParseExact(dateEnd, "dd-MM-yyyy", CultureInfo.InvariantCulture);
            return new ScheduleViewResult
            {
                Schedule = ScheduleManagementService.GetUserSchedule(UserContext.CurrentUserId, dateTimeStart, dateTimeEnd).Select(x => new ScheduleViewData(x))
            };
        }

        public ScheduleViewResult GetScheduleBetweenTime(string date, string startTime, string endTime)
        {
            var dateTime = DateTime.ParseExact(date, "dd-MM-yyyy", CultureInfo.InvariantCulture);
            var start = DateTime.ParseExact(startTime, "HH:mm", CultureInfo.InvariantCulture).TimeOfDay;
            var end = DateTime.ParseExact(endTime, "HH:mm", CultureInfo.InvariantCulture).TimeOfDay;
            return new ScheduleViewResult
            {
                Schedule = ScheduleManagementService.GetScheduleBetweenTimes(dateTime, start, end).Select(x => new ScheduleViewData(x))
            };
        }

        public ResultViewData DeleteLabScheduleDate(int id)
        {
            try
            {
                SubjectManagementService.DeleteLabsVisitingDate(id);

                return new ResultViewData
                {
                    Message = "Дата успешно удалена",
                    Code = "200"
                };
            }
            catch (Exception)
            {
                return new ResultViewData
                {
                    Message = "Произошла ошибка при удалении даты",
                    Code = "500"
                };
            }
        }

        public ResultViewData DeleteLectureScheduleDate(int id)
        {
            try
            {
                SubjectManagementService.DeleteLectionVisitingDate(id);

                return new ResultViewData
                {
                    Message = "Дата успешно удалена",
                    Code = "200"
                };
            }
            catch
            {
                return new ResultViewData
                {
                    Message = "Произошла ошибка при удалении даты",
                    Code = "500"
                };
            }
        }

        public ResultViewData DeletePracticalScheduleDate(int id)
        {
            try
            {
                PracticalManagementService.DeletePracticalScheduleDate(id);

                return new ResultViewData
                {
                    Message = "Дата успешно удалена",
                    Code = "200"
                };
            }
            catch (Exception)
            {
                return new ResultViewData
                {
                    Message = "Произошла ошибка при удалении даты",
                    Code = "500"
                };
            }
        }

        private ScheduleBase SaveDateValidate(int id, int subjectId, string date, string startTime, string endTime, string building, string audience, Note note, int? lecturerId)
        {
            var isUserAssigned = SubjectManagementService.IsUserAssignedToSubjectAndLector(UserContext.CurrentUserId, subjectId);
            if (!isUserAssigned)
            {
                throw new ArgumentException("Пользователь не добавлен к предмету");
            }
            var lectors = LecturerManagementService.GetJoinedLector(subjectId)
                .GroupBy(x => x.Id).Select(x => x.First());


            if (lecturerId.HasValue && !lectors.Any(x => x.Id == lecturerId))
            {
                throw new ArgumentException("Преподаватель не добавлен к предмету");
            }
            var dateTime = DateTime.ParseExact(date, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            var start = DateTime.ParseExact(startTime, "HH:mm", CultureInfo.InvariantCulture).TimeOfDay;
            var end = DateTime.ParseExact(endTime, "HH:mm", CultureInfo.InvariantCulture).TimeOfDay;
            var busySchedules = ScheduleManagementService.CheckIfAllowed(dateTime, start, end, building, audience).ToList();
            if (busySchedules.Any())
            {
                throw new ScheduleBusyException{ Schedule = busySchedules};

            }
            var scheduleDate = new ScheduleBase
            {
                Id = id,
                Audience = audience,
                Date = dateTime,
                Building = building,
                EndTime = end,
                StartTime = start,
                LecturerId = lecturerId
            };
            if (note != null)
            {
                note.UserId = UserContext.CurrentUserId;
                scheduleDate.Notes.Add(note);
            }

            return scheduleDate;
        }
    }
}
