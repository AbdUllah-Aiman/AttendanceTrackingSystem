﻿using AttendanceTrackingSystem.Models;

namespace AttendanceTrackingSystem.IRepository
{
    public interface IRepoPermission
    {
        public List<Permission> getAll();
        public Permission getById(int id);
        public void Add(Permission permission);
        public void Update(Permission permission);
        public void Delete(int id);
        public List<Permission> GetAllBySupervisorId(int supervisorId);
        public List<Permission> getAllById(int userId);
		public bool CheckPermission(int studentId, DateTime date);

        public string CheckSchedule(int studentId, DateTime date);
	}
}
