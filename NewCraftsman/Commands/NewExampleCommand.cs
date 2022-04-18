namespace NewCraftsman.Commands;

using System.IO.Abstractions;
using Builders;
using Domain;
using Domain.DomainProject;
using Domain.DomainProject.Dtos;
using Helpers;
using Spectre.Console;
using Spectre.Console.Cli;
using static Helpers.ConsoleWriter;

public class NewExampleCommand : Command<NewExampleCommand.Settings>
{
    private IAnsiConsole _console;
    private readonly IFileSystem _fileSystem;
    private readonly IConsoleWriter _consoleWriter;
    private readonly ICraftsmanUtilities _utilities;

    public NewExampleCommand(IAnsiConsole console, IFileSystem fileSystem, IConsoleWriter consoleWriter, ICraftsmanUtilities utilities)
    {
        _console = console;
        _fileSystem = fileSystem;
        _consoleWriter = consoleWriter;
        _utilities = utilities;
    }

    public class Settings : CommandSettings
    {
        [CommandArgument(0, "[ProjectName]")]
        public string ProjectName { get; set; }
    }
    
    public override int Execute(CommandContext context, Settings settings)
    {
        var rootDir = _fileSystem.Directory.GetCurrentDirectory();
        var myEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
          
        if (myEnv == "Dev")
          rootDir = _console.Ask<string>("Enter the root directory of your project:");
        
        var (exampleType, projectName) = RunPrompt(settings.ProjectName);
        var templateString = GetExampleDomain(projectName, exampleType);
                
        var domainProjectDto = FileParsingHelper.ReadYamlString<DomainProjectDto>(templateString);
        var domainProject = DomainProject.Create(domainProjectDto);
        
        var solutionDirectory = $"{rootDir}{Path.DirectorySeparatorChar}{domainProject.DomainName}";
        
        var domainCommand = new NewDomainCommand(_console, _fileSystem, _consoleWriter, _utilities);
        domainCommand.CreateNewDomainProject(solutionDirectory, domainProject);
        
        // TODO add this back
        // ExampleTemplateBuilder.CreateYamlFile(domainDirectory, templateString, fileSystem);
        _console.MarkupLine($"{Environment.NewLine}[bold yellow1]Your example project is project is ready![/]");

        _consoleWriter.StarGithubRequest();
        return 0;
    }
    
    private (ExampleType type, string name) RunPrompt(string projectName)
    {
        _console.WriteLine();
        _console.Write(new Rule("[yellow]Create an Example Project[/]").RuleStyle("grey").Centered());

        var typeString = AskExampleType();
        var exampleType = ExampleType.FromName(typeString, ignoreCase: true);
        if(string.IsNullOrEmpty(projectName))
          projectName = AskExampleProjectName();

        return (exampleType, projectName);
    }

    private string AskExampleType()
    {
        var exampleTypes = ExampleType.List.Select(e => e.Name);
        
        return _console.Prompt(
            new SelectionPrompt<string>()
                .Title("What [green]type of example[/] do you want to create?")
                .PageSize(50)
                .AddChoices(exampleTypes)
        );
    }

    private string AskExampleProjectName()
    {
        return _console.Ask<string>("What would you like to name this project (e.g. [green]MyExampleProject[/])?");
    }
        
    private static string GetExampleDomain(string name, ExampleType exampleType)
    {
        if (exampleType == ExampleType.Basic)
            return BasicTemplate(name);
        if (exampleType == ExampleType.WithAuth)
            return AuthTemplate(name);
        if(exampleType == ExampleType.WithBus)
            return BusTemplate(name);
        if(exampleType == ExampleType.WithAuthServer) 
          return AuthServerTemplate(name);
        if(exampleType == ExampleType.WithForeignKey) 
          return ForeignKeyTemplate(name);
        if(exampleType == ExampleType.Complex) 
          return ComplexTemplate(name);

        throw new Exception("Example type was not recognized.");
    }
        
        private static string ForeignKeyTemplate(string name)
        {
          return $@"DomainName: {name}
BoundedContexts:
- ProjectName: RecipeManagement
  Port: 5375
  DbContext:
   ContextName: RecipesDbContext
   DatabaseName: RecipeManagement
   Provider: Postgres
  Entities:
  - Name: Recipe
    Features:
    - Type: GetList
    - Type: GetRecord
    - Type: AddRecord
    - Type: UpdateRecord
    - Type: DeleteRecord
    Properties:
    - Name: Title
      Type: string
      CanFilter: true
      CanSort: true
    - Name: Directions
      Type: string
      CanFilter: true
      CanSort: true
    - Name: Author
      Type: Author
      ForeignEntityName: Author
      ForeignEntityPlural: Authors
    - Name: Ingredients
      Type: ICollection<Ingredient>
      ForeignEntityPlural: Ingredients
  - Name: Author
    Features:
    - Type: GetList
    - Type: GetRecord
    - Type: AddRecord
    - Type: UpdateRecord
    - Type: DeleteRecord
    Properties:
    - Name: Name
      Type: string
      CanFilter: true
      CanSort: true
    - Name: RecipeId
      Type: Guid
      ForeignEntityName: Recipe
      ForeignEntityPlural: Recipes
  - Name: Ingredient
    Features:
    - Type: GetList
    - Type: GetRecord
    - Type: AddRecord
    - Type: UpdateRecord
    - Type: DeleteRecord
    - Type: AddListByFk
      BatchPropertyName: RecipeId
      BatchPropertyType: Guid
      ParentEntity: Recipe
      BatchPropertyDbSetName: Recipes
    Properties:
    - Name: Name
      Type: string
      CanFilter: true
      CanSort: true
    - Name: Quantity
      Type: string
      CanFilter: true
      CanSort: true
    - Name: Measure
      Type: string
      CanFilter: true
      CanSort: true
    - Name: RecipeId
      Type: Guid
      ForeignEntityName: Recipe";
        }
        
        private static string ComplexTemplate(string name)
        {
          return $@"DomainName: {name}
BoundedContexts:
- ProjectName: RecipeManagement
  Port: 5375
  DbContext:
   ContextName: RecipesDbContext
   DatabaseName: RecipeManagement
   Provider: Postgres
  Entities:
  - Name: Recipe
    Features:
    - Type: GetList
      IsProtected: true
      PermissionName: CanReadRecipes
    - Type: GetRecord
      IsProtected: true
      PermissionName: CanReadRecipes
    - Type: AddRecord
      IsProtected: true
    - Type: UpdateRecord
      IsProtected: true
    - Type: DeleteRecord
      IsProtected: true
    - Type: PatchRecord
      IsProtected: true
    Properties:
    - Name: Title
      Type: string
      CanFilter: true
      CanSort: true
    - Name: Directions
      Type: string
      CanFilter: true
      CanSort: true
    - Name: Rating
      Type: int?
      CanFilter: true
      CanSort: true
    - Name: Author
      Type: Author
      ForeignEntityName: Author
      ForeignEntityPlural: Authors
    - Name: Ingredients
      Type: ICollection<Ingredient>
      ForeignEntityPlural: Ingredients
  - Name: Author
    Features:
    - Type: GetList
    - Type: GetRecord
    - Type: AddRecord
    - Type: UpdateRecord
    - Type: DeleteRecord
    Properties:
    - Name: Name
      Type: string
      CanFilter: true
      CanSort: true
    - Name: RecipeId
      Type: Guid
      ForeignEntityName: Recipe
      ForeignEntityPlural: Recipes
  - Name: Ingredient
    Features:
    - Type: GetList
    - Type: GetRecord
    - Type: AddRecord
    - Type: UpdateRecord
    - Type: DeleteRecord
    - Type: AddListByFk
      BatchPropertyName: RecipeId
      BatchPropertyType: Guid
      ParentEntity: Recipe
      BatchPropertyDbSetName: Recipes
    Properties:
    - Name: Name
      Type: string
      CanFilter: true
      CanSort: true
    - Name: Quantity
      Type: string
      CanFilter: true
      CanSort: true
    - Name: Measure
      Type: string
      CanFilter: true
      CanSort: true
    - Name: RecipeId
      Type: Guid
      ForeignEntityName: Recipe
  Environment:
      AuthSettings:
        Authority: https://localhost:3385
        Audience: recipe_management
        AuthorizationUrl: https://localhost:3385/connect/authorize
        TokenUrl: https://localhost:3385/connect/token
        ClientId: recipe_management.swagger
        ClientSecret: 974d6f71-d41b-4601-9a7a-a33081f80687
      BrokerSettings:
        Host: localhost
        VirtualHost: /
        Username: guest
        Password: guest
  Bus:
    AddBus: true
  Producers:
  - EndpointRegistrationMethodName: AddRecipeProducerEndpoint
    ProducerName: AddRecipeProducer
    ExchangeName: recipe-added
    MessageName: IRecipeAdded
    DomainDirectory: Recipes
    ExchangeType: fanout
    UsesDb: true
  Consumers:
  - EndpointRegistrationMethodName: AddToBookEndpoint
    ConsumerName: AddToBook
    ExchangeName: book-additions
    QueueName: add-recipe-to-book
    MessageName: IRecipeAdded
    DomainDirectory: Recipes
    ExchangeType: fanout
Messages:
- Name: IRecipeAdded
  Properties:
  - Name: RecipeId
    Type: guid
Bff:
  ProjectName: RecipeManagementApp
  ProxyPort: 4378
  HeadTitle: Recipe Management App
  Authority: https://localhost:3385
  ClientId: recipe_management.bff
  ClientSecret: 974d6f71-d41b-4601-9a7a-a33081f80687
  RemoteEndpoints:
    - LocalPath: /api/recipes
      ApiAddress: https://localhost:5375/api/recipes
    - LocalPath: /api/ingredients
      ApiAddress: https://localhost:5375/api/ingredients
  BoundaryScopes:
    - recipe_management
  Entities:
  - Name: Recipe
    Features:
    - Type: GetList
    - Type: GetRecord
    - Type: AddRecord
    - Type: UpdateRecord
    - Type: DeleteRecord
    Properties:
    - Name: Title
      Type: string #optional if string
    - Name: Directions
    - Name: RecipeSourceLink
    - Name: Description
    - Name: Rating
      Type: number?
  - Name: Ingredient
    Features:
    - Type: GetList
    - Type: GetRecord
    - Type: AddRecord
    - Type: UpdateRecord
    - Type: DeleteRecord
    Properties:
    - Name: Name
    - Name: Quantity
    - Name: Measure
    - Name: RecipeId
AuthServer:
  Name: AuthServerWithDomain
  Port: 3385
  Clients:
    - Id: recipe_management.swagger
      Name: RecipeManagement Swagger
      Secrets:
        - 974d6f71-d41b-4601-9a7a-a33081f80687
      GrantType: Code
      RedirectUris:
        - 'https://localhost:5375/swagger/oauth2-redirect.html'
      PostLogoutRedirectUris:
        - 'http://localhost:5375/'
      AllowedCorsOrigins:
        - 'https://localhost:5375'
      FrontChannelLogoutUri: 'http://localhost:5375/signout-oidc'
      AllowOfflineAccess: true
      RequirePkce: true
      RequireClientSecret: true
      AllowPlainTextPkce: false
      AllowedScopes:
        - openid
        - profile
        - role
        - recipe_management #this should match the scope in your boundary's swagger spec 
    - Id: recipe_management.bff
      Name: RecipeManagement BFF
      Secrets:
        - 974d6f71-d41b-4601-9a7a-a33081f80687
      GrantType: Code
      RedirectUris:
        - https://localhost:4378/signin-oidc
      PostLogoutRedirectUris:
        - https://localhost:4378/signout-callback-oidc
      AllowedCorsOrigins:
        - https://localhost:5375
        - https://localhost:4378
      FrontChannelLogoutUri: https://localhost:4378/signout-oidc
      AllowOfflineAccess: true
      RequirePkce: true
      RequireClientSecret: true
      AllowPlainTextPkce: false
      AllowedScopes:
        - openid
        - profile
        - role
        - recipe_management #this should match the scope in your boundary's swagger spec 
  Scopes:
    - Name: recipe_management
      DisplayName: Recipes Management - API Access
  Apis:
    - Name: recipe_management
      DisplayName: Recipe Management
      ScopeNames:
        - recipe_management
      Secrets:
        - 4653f605-2b36-43eb-bbef-a93480079f20
      UserClaims:
        - openid
        - profile
        - role";
        }

        private static string BasicTemplate(string name)
        {
            return $@"DomainName: {name}";
            return $@"DomainName: {name}
BoundedContexts:
- ProjectName: RecipeManagement
  Port: 5375
  DbContext:
   ContextName: RecipesDbContext
   DatabaseName: RecipeManagement
   Provider: postgres
   NamingConvention: class
  Entities:
  - Name: Recipe
    Features:
    - Type: GetList
    - Type: GetRecord
    - Type: AddRecord
    - Type: UpdateRecord
    - Type: DeleteRecord
    Properties:
    - Name: Title
      Type: string
      CanFilter: true
      CanSort: true
    - Name: Directions
      Type: string
      CanFilter: true
      CanSort: true
    - Name: RecipeSourceLink
      Type: string
      CanFilter: true
      CanSort: true
    - Name: Description
      Type: string
      CanFilter: true
      CanSort: true
    - Name: ImageLink
      Type: string
      CanFilter: true
      CanSort: true";
        }
        
        private static string AuthTemplate(string name)
        {
            return $@"DomainName: {name}
BoundedContexts:
- ProjectName: RecipeManagement
  Port: 5375
  DbContext:
    ContextName: RecipesDbContext
    DatabaseName: RecipeManagement
    Provider: postgres
  Entities:
  - Name: Recipe
    Features:
    - Type: GetList
      IsProtected: true
      PermissionName: CanReadRecipes
    - Type: GetRecord
      IsProtected: true
      PermissionName: CanReadRecipes
    - Type: AddRecord
      IsProtected: true
    - Type: UpdateRecord
      IsProtected: true
    - Type: DeleteRecord
      IsProtected: true
    Properties:
    - Name: Title
      Type: string
      CanFilter: true
      CanSort: true
    - Name: Directions
      Type: string
      CanFilter: true
      CanSort: true
    - Name: RecipeSourceLink
      Type: string
      CanFilter: true
      CanSort: true
    - Name: Description
      Type: string
      CanFilter: true
      CanSort: true
    - Name: ImageLink
      Type: string
      CanFilter: true
      CanSort: true
  Environment:
    AuthSettings:
      Authority: https://localhost:5010
      Audience: recipeManagementDev
      AuthorizationUrl: https://localhost:5010/connect/authorize
      TokenUrl: https://localhost:5010/connect/token
      ClientId: service.client";
        }

        private static string BusTemplate(string name)
        {
            var template = $@"DomainName: {name}
BoundedContexts:
- ProjectName: RecipeManagement
  Port: 5375
  DbContext:
   ContextName: RecipesDbContext
   DatabaseName: RecipeManagement
   Provider: Postgres
  Entities:
  - Name: Recipe
    Features:
    - Type: GetList
    - Type: GetRecord
    - Type: AddRecord
    - Type: UpdateRecord
    - Type: DeleteRecord
    Properties:
    - Name: Title
      Type: string
      CanFilter: true
      CanSort: true
    - Name: Directions
      Type: string
      CanFilter: true
      CanSort: true
    - Name: RecipeSourceLink
      Type: string
      CanFilter: true
      CanSort: true
    - Name: Description
      Type: string
      CanFilter: true
      CanSort: true
    - Name: ImageLink
      Type: string
      CanFilter: true
      CanSort: true
  Environment:
      BrokerSettings:
        Host: localhost
        VirtualHost: /
        Username: guest
        Password: guest
  Bus:
    AddBus: true
  Producers:
  - EndpointRegistrationMethodName: AddRecipeProducerEndpoint
    ProducerName: AddRecipeProducer
    ExchangeName: recipe-added
    MessageName: IRecipeAdded
    DomainDirectory: Recipes
    ExchangeType: fanout
    UsesDb: true
  Consumers:
  - EndpointRegistrationMethodName: AddToBookEndpoint
    ConsumerName: AddToBook
    ExchangeName: book-additions
    QueueName: add-recipe-to-book
    MessageName: IRecipeAdded
    DomainDirectory: Recipes
    ExchangeType: fanout
Messages:
- Name: IRecipeAdded
  Properties:
  - Name: RecipeId
    Type: guid";

            return template;
        }

        private static string AuthServerTemplate(string name)
        {
          return $@"DomainName: {name}
BoundedContexts:
- ProjectName: RecipeManagement
  Port: 5375
  DbContext:
    ContextName: RecipesDbContext
    DatabaseName: RecipeManagement
    Provider: Postgres
  Entities:
  - Name: Recipe
    Features:
    - Type: GetList
      IsProtected: true
      PermissionName: CanReadRecipes
    - Type: GetRecord
      IsProtected: true
      PermissionName: CanReadRecipes
    - Type: AddRecord
      IsProtected: true
    - Type: UpdateRecord
      IsProtected: true
    - Type: DeleteRecord
      IsProtected: true
    Properties:
    - Name: Title
      Type: string
      CanFilter: true
      CanSort: true
    - Name: Directions
      Type: string
      CanFilter: true
      CanSort: true
    - Name: RecipeSourceLink
      Type: string
      CanFilter: true
      CanSort: true
    - Name: Description
      Type: string
      CanFilter: true
      CanSort: true
    - Name: ImageLink
      Type: string
      CanFilter: true
      CanSort: true
    - Name: Rating
      Type: int?
      CanFilter: true
      CanSort: true
  Environment:
    AuthSettings:
      Authority: https://localhost:3385
      Audience: recipe_management
      AuthorizationUrl: https://localhost:3385/connect/authorize
      TokenUrl: https://localhost:3385/connect/token
      ClientId: recipe_management.swagger
      ClientSecret: 974d6f71-d41b-4601-9a7a-a33081f80687
Bff:
  ProjectName: RecipeManagementApp
  ProxyPort: 4378
  HeadTitle: Recipe Management App
  Authority: https://localhost:3385
  ClientId: recipe_management.bff
  ClientSecret: 974d6f71-d41b-4601-9a7a-a33081f80687
  RemoteEndpoints:
    - LocalPath: /api/recipes
      ApiAddress: https://localhost:5375/api/recipes
  BoundaryScopes:
    - recipe_management
  Entities:
  - Name: Recipe
    Features:
    - Type: GetList
    - Type: GetRecord
    - Type: AddRecord
    - Type: UpdateRecord
    - Type: DeleteRecord
    Properties:
    - Name: Title
      Type: string #optional if string
    - Name: Directions
    - Name: RecipeSourceLink
    - Name: Description
    - Name: ImageLink
    - Name: Rating
      Type: number?
AuthServer:
  Name: AuthServerWithDomain
  Port: 3385
  Clients:
    - Id: recipe_management.swagger
      Name: RecipeManagement Swagger
      Secrets:
        - 974d6f71-d41b-4601-9a7a-a33081f80687
      GrantType: Code
      RedirectUris:
        - 'https://localhost:5375/swagger/oauth2-redirect.html'
      PostLogoutRedirectUris:
        - 'http://localhost:5375/'
      AllowedCorsOrigins:
        - 'https://localhost:5375'
      FrontChannelLogoutUri: 'http://localhost:5375/signout-oidc'
      AllowOfflineAccess: true
      RequirePkce: true
      RequireClientSecret: true
      AllowPlainTextPkce: false
      AllowedScopes:
        - openid
        - profile
        - role
        - recipe_management #this should match the scope in your boundary's swagger spec
    - Id: recipe_management.bff
      Name: RecipeManagement BFF
      Secrets:
        - 974d6f71-d41b-4601-9a7a-a33081f80687
      GrantType: Code
      RedirectUris:
        - https://localhost:4378/signin-oidc
      PostLogoutRedirectUris:
        - https://localhost:4378/signout-callback-oidc
      AllowedCorsOrigins:
        - https://localhost:5375
        - https://localhost:4378
      FrontChannelLogoutUri: https://localhost:4378/signout-oidc
      AllowOfflineAccess: true
      RequirePkce: true
      RequireClientSecret: true
      AllowPlainTextPkce: false
      AllowedScopes:
        - openid
        - profile
        - role
        - recipe_management #this should match the scope in your boundary's swagger spec 
  Scopes:
    - Name: recipe_management
      DisplayName: Recipes Management - API Access
  Apis:
    - Name: recipe_management
      DisplayName: Recipe Management
      ScopeNames:
        - recipe_management
      Secrets:
        - 4653f605-2b36-43eb-bbef-a93480079f20
      UserClaims:
        - openid
        - profile
        - role
";
        }
    }