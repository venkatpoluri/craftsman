﻿namespace NewCraftsman.Builders.Projects
{
    using System.IO.Abstractions;

    public class SharedKernelCsProjBuilder
    {
        public static void CreateMessagesCsProj(string solutionDirectory, IFileSystem fileSystem)
        {
            var classPath = ClassPathHelper.SharedKernelProjectClassPath(solutionDirectory);
            var fileText = GetMessagesCsProjFileText();
            Utilities.CreateFile(classPath, fileText, fileSystem);
        }

        public static string GetMessagesCsProjFileText()
        {
            return @$"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""FluentValidation"" Version=""10.4.0"" />
  </ItemGroup>

</Project>";
        }
    }
}