﻿namespace Craftsman.Builders
{
    using Craftsman.Builders.Dtos;
    using Craftsman.Enums;
    using Craftsman.Exceptions;
    using Craftsman.Helpers;
    using Craftsman.Models;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection.Emit;
    using System.Text;
    using static Helpers.ConsoleWriter;

    public class DbContextBuilder
    {
        public static void CreateDbContext(string solutionDirectory, ApiTemplate template)
        {
            try
            {
                var classPath = ClassPathHelper.DbContextClassPath(solutionDirectory, $"{template.DbContext.ContextName}.cs");

                if (!Directory.Exists(classPath.ClassDirectory))
                    Directory.CreateDirectory(classPath.ClassDirectory);

                if (File.Exists(classPath.FullClassPath))
                    throw new FileAlreadyExistsException(classPath.FullClassPath);

                using (FileStream fs = File.Create(classPath.FullClassPath))
                {
                    var data = GetContextFileText(classPath.ClassNamespace, template);
                    fs.Write(Encoding.UTF8.GetBytes(data));
                }

                RegisterContext(solutionDirectory, template);

                GlobalSingleton.AddCreatedFile(classPath.FullClassPath.Replace($"{solutionDirectory}\\", ""));
            }
            catch (FileAlreadyExistsException e)
            {
                WriteError(e.Message);
                throw;
            }
            catch (Exception e)
            {
                WriteError($"An unhandled exception occured when running the API command.\nThe error details are: \n{e.Message}");
                throw;
            }
        }

        private static string GetContextFileText(string classNamespace, ApiTemplate template)
        {
            return @$"namespace {classNamespace}
{{
    using Application.Interfaces;
    using Domain.Common;
    using Domain.Entities;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.ChangeTracking;
    using System.Threading;
    using System.Threading.Tasks;

    public class {template.DbContext.ContextName} : DbContext
    {{
        private readonly IDateTimeService _dateTimeService;

        public {template.DbContext.ContextName}(
            DbContextOptions<{template.DbContext.ContextName}> options,
            IDateTimeService dateTime) : base(options) 
        {{
            _dateTimeService = dateTime;
        }}

        #region DbSet Region - Do Not Delete
{GetDbSetText(template.Entities)}
        #endregion

        //TODO: Abstract this logic out into an custom inheritable dbcontext
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
        {{
            foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
            {{
                UpdateAuditableFields(entry, _dateTimeService);
            }}

            return base.SaveChangesAsync(cancellationToken);
        }}

        public override int SaveChanges()
        {{
            foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
            {{
                UpdateAuditableFields(entry, _dateTimeService);
            }}

            return base.SaveChanges();
        }}

        public static void UpdateAuditableFields(EntityEntry<AuditableEntity> entry, IDateTimeService service)
        {{
            switch (entry.State)
            {{
                case EntityState.Added:
                    entry.Entity.CreatedBy = ""TBD User""; //_currentUserService.UserId;
                    entry.Entity.CreatedOn = service.NowUtc;
                    break;
                case EntityState.Modified:
                    entry.Entity.LastModifiedBy = ""TBD User""; //_currentUserService.UserId;
                    entry.Entity.LastModifiedOn = service.NowUtc;
                    break;
            }}            
        }}
    }}
}}";
        }

        public static string GetDbSetText(List<Entity> entities)
        {
            var dbSetText = "";

            foreach(var entity in entities)
            {
                var newLine = entity == entities.LastOrDefault() ? "" : $"{Environment.NewLine}";
                dbSetText += @$"        public DbSet<{entity.Name}> {entity.Plural} {{ get; set; }}{newLine}";
            }

            return dbSetText;
        }

        private static void RegisterContext(string solutionDirectory, ApiTemplate template)
        {
            var classPath = ClassPathHelper.DbContextClassPath(solutionDirectory, $"{template.DbContext.ContextName}.cs");

            if (!Directory.Exists(classPath.ClassDirectory))
                Directory.CreateDirectory(classPath.ClassDirectory);

            if (!File.Exists(classPath.FullClassPath))
                throw new FileNotFoundException($"The `{classPath.ClassDirectory}` file could not be found.");

            var tempPath = $"{classPath.FullClassPath}temp";
            using (var input = File.OpenText(classPath.FullClassPath))
            {
                using (var output = new StreamWriter(tempPath))
                {
                    string line;
                    while (null != (line = input.ReadLine()))
                    {
                        var newText = $"{line}";
                        if (line.Contains("#region DbContext"))
                        {
                            newText += @$"          
            if (configuration.GetValue<bool>(""UseInMemoryDatabase""))
            {{
                services.AddDbContext<{template.DbContext.ContextName}>(options =>
                    options.UseInMemoryDatabase($""{template.DbContext.DatabaseName ?? template.DbContext.ContextName}""));
            }}
            else
            {{
                services.AddDbContext<{template.DbContext.ContextName}>(options =>
                    options.UseSqlServer(
                        configuration.GetConnectionString(""DefaultConnection""),
                        builder => builder.MigrationsAssembly(typeof({template.DbContext.ContextName}).Assembly.FullName)));
            }}";
                        }

                        output.WriteLine(newText);
                    }
                }
            }

            // delete the old file and set the name of the new one to the original nape
            File.Delete(classPath.FullClassPath);
            File.Move(tempPath, classPath.FullClassPath);

            GlobalSingleton.AddUpdatedFile(classPath.FullClassPath.Replace($"{solutionDirectory}\\", ""));
        }
    }
}