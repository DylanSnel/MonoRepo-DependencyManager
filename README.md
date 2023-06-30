# Monorepo-DependencyManager

This tool was created to allow dotnet projects to use a monorepo structure on azure devops. It tries to solve the problem of having shared code betweeen solutions and knowing when to run pipelines to check if the users of shared code do not break when shared code is changed.

It has a few features that can be enabled trough configuration. But the update command of the application functions like this:

- Find all Solutions (.sln) in the git repository
- Per solution find all the Projects (that do not end in Test.csproj)
- For every project build a dependency tree of other projects
- For all the projects in the tree:
    - **[Feature]**: Find docker files and refer all projects for caching
    - For Azure Devops pipeline yml files.
        - **[Feature]**: Update the trigger paths acccording to the dependency tree
        - **[Feature]**: Import the pipeline into Azure Devops
        - **[Feature]**: Create branch policies for specific branches 

## Installation

The tool can be easily installed using dotnet tool since it is hosted on [Nuget](https://www.nuget.org/packages/Monorepo.DependencyManager/)

Command:  ``` dotnet tool install --global MonoRepo.DependencyManager ```


## How to use?

The depenency manager will only function when run inside a folder that is (part of) a git repository. It will automatically inspect the current branch and the root of the current repoository.

To use Azure Devops 

### Help

Command:  ``` MonoRepo --help ```

Displays the list of commands and a short description of that they do.

### Init

Command:  ``` MonoRepo init [--overwrite] ```

Starts the MonoRepo initialization process. The tool will help you configure itself to do excactly what you need. It will create a ```.monorepo-config``` file in the root of the repository to store its configuration. These configurations do not contain any sensitive data.

#### Options
``` --overwrite ```
Will allow you to overwrite an existing configuration to make changes.

### Check Permissions

Command:  ```MonoRepo check-permissions```

Runs a small diagnostics run to see if the neccesary tools are installed to use Azure DevOps functionalities:
- Azure Cli
- Azure DevOps Cli Extension
- Check if an acces token can be retrieved

### Update

Command:  ``` MonoRepo update [--pat <Devops Personal Access Token>] [-v] ```

Starts depencency update flow:
- Find all Solutions (.sln) in the git repository
- Per solution find all the Projects (that do not end in Test.csproj)
- For every project build a dependency tree of other projects
- For all the projects in the tree:
    - **[Feature]**: Find docker files and refer all projects for caching
    - For Azure Devops pipeline yml files.
        - **[Feature]**: Update the trigger paths acccording to the dependency tree
        - **[Feature]**: Import the pipeline into Azure Devops
        - **[Feature]**: Create branch policies for specific branches 

#### Options
``` --pat ```
Use an Azure Devops PAT instead of Azure Cli login

``` -v ```
Verbose

### Tree

Command:  ``` MonoRepo update [--pat <Devops Personal Access Token>] ```

#### Options
``` --pat ```
Use an Azure Devops PAT instead of Azure Cli login








