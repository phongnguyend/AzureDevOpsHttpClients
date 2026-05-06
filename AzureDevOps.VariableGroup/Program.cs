using AzureDevOps.VariableGroup;
using Microsoft.Extensions.Configuration;

var builder = new ConfigurationBuilder()
    //.AddJsonFile("appsettings.json")
    .AddUserSecrets<Program>();

var configuration = builder.Build();

var connectionInfo = new ConnectionInfo();

configuration.Bind(connectionInfo);

var client = new VariableGroupClient(connectionInfo);

// List all variable groups
var groups = await client.ListVariableGroupsAsync();
Console.WriteLine($"Found {groups.Count} variable group(s):");
foreach (var g in groups)
    Console.WriteLine($"  [{g.Id}] {g.Name} - {g.Variables?.Count ?? 0} variable(s)");

// Create a variable group
var created = await client.CreateVariableGroupAsync(new CreateVariableGroupRequest
{
    Name = "MyVariableGroup_" + Guid.CreateVersion7(),
    Description = "Created via API",
    Variables = new()
    {
        ["MyVar1"] = new VariableValue { Value = "Value1" },
        ["MySecret"] = new VariableValue { Value = "SecretValue", IsSecret = true }
    }
});
Console.WriteLine($"\nCreated group: [{created.Id}] {created.Name}");

// List variables in the created group
var variables = await client.ListVariablesAsync(created.Id);
Console.WriteLine($"\nVariables in group '{created.Name}':");
foreach (var (name, val) in variables)
    Console.WriteLine($"  {name} = {(val.IsSecret ? "***" : val.Value)}");

// Add a new variable
var afterAdd = await client.CreateVariableAsync(created.Id, "NewVar", new VariableValue { Value = "NewValue" });
Console.WriteLine($"\nAfter adding 'NewVar': {afterAdd.Variables.Count} variable(s)");

// Update an existing variable
var afterUpdate = await client.UpdateVariableAsync(created.Id, "MyVar1", new VariableValue { Value = "UpdatedValue" });
Console.WriteLine($"\nAfter updating 'MyVar1': value = {afterUpdate.Variables["MyVar1"].Value}");

// Delete a variable
var afterDelete = await client.DeleteVariableAsync(created.Id, "NewVar");
Console.WriteLine($"\nAfter deleting 'NewVar': {afterDelete.Variables.Count} variable(s)");

// Update the group
var updated = await client.UpdateVariableGroupAsync(created.Id, new UpdateVariableGroupRequest
{
    Name = created.Name + "-Updated",
    Description = "Updated via API",
    Type = created.Type,
    Variables = created.Variables
});
Console.WriteLine($"\nUpdated group name: {updated.Name}");

// Delete the group
var projectId = created.VariableGroupProjectReferences.First().ProjectReference.Id;
await client.DeleteVariableGroupAsync(created.Id, projectId);

Console.WriteLine($"\nDeleted group [{created.Id}]");

Console.WriteLine("\nPress any key to continue ...");
Console.ReadLine();
