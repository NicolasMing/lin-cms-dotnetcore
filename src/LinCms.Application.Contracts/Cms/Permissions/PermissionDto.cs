﻿using LinCms.Entities;

namespace LinCms.Cms.Permissions
{
    public class PermissionDto:EntityDto<long>
    {
        public PermissionDto(string name, string module)
        {
            Name = name;
            Module = module;
        }

        public string Name { get; set; }
        public string Module { get; set; }
    }
}
