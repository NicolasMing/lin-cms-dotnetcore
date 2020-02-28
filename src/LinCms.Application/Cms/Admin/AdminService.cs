﻿using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using LinCms.Application.Contracts.Cms.Permissions;
using LinCms.Core.Entities;
using LinCms.Infrastructure.Repositories;

namespace LinCms.Application.Cms.Admin
{
    public class AdminService : IAdminService
    {
        private readonly AuditBaseRepository<LinPermission> _permissionRepository;
        private readonly IMapper _mapper;
        public AdminService(AuditBaseRepository<LinPermission> permissionRepository, IMapper mapper)
        {
            _permissionRepository = permissionRepository;
            _mapper = mapper;
        }

        public IDictionary<string, List<PermissionDto>> GetAllStructualPermissions()
        {
            return _permissionRepository.Select.ToList()
                 .GroupBy(r => r.Module)
                 .ToDictionary(
                     group => group.Key,
                     group =>
                         _mapper.Map<List<PermissionDto>>(group.ToList())
                   );

        }
    }
}
