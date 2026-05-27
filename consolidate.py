import os
import shutil
import subprocess

# List of projects to move
projects = [
    "ConectionManager",
    "DocumentManager",
    "SchemaDiscovery",
    "TestCreator",
    "TestCreatorWpfApp",
    "UdpTerminalTool",
    "Test Files"
]

root_dir = os.getcwd()
parent_dir = os.path.dirname(root_dir)

print(f"Root: {root_dir}")
print(f"Parent: {parent_dir}")

for project in projects:
    src = os.path.join(parent_dir, project)
    dst = os.path.join(root_dir, project)
    
    if os.path.exists(src):
        print(f"Moving {project} to {dst}...")
        # If destination exists (e.g. from previous attempt), remove it
        if os.path.exists(dst):
            shutil.rmtree(dst)
        shutil.copytree(src, dst)
        print(f"Successfully copied {project}")
    else:
        print(f"Project {project} not found in parent directory.")

# Update the solution file to point to local directories instead of relative parent
sln_path = os.path.join(root_dir, "EnbeddedRequirmentToTest.sln")
if os.path.exists(sln_path):
    with open(sln_path, 'r', encoding='utf-8') as f:
        sln_content = f.read()
    
    # Replace "..\" with "" for all projects we just moved
    # Note: We need to be careful not to break other things, but projects are usually like "..\Project\Project.csproj"
    new_sln_content = sln_content.replace(r'..\\', '')
    
    with open(sln_path, 'w', encoding='utf-8', newline='') as f:
        f.write(new_sln_content)
    print("Updated Solution file references.")

# Update Project References in .csproj files
for root, dirs, files in os.walk(root_dir):
    for file in files:
        if file.endswith(".csproj"):
            file_path = os.path.join(root, file)
            with open(file_path, 'r', encoding='utf-8') as f:
                csproj_content = f.read()
            
            if r'Include="..\\' in csproj_content:
                new_csproj = csproj_content.replace(r'Include="..\\', r'Include="..\\..\\') # This is for sub-sub projects
                # Actually, most references should now be sibling folders within root.
                # If a project is at root/Project1 and refers to root/Project2, it was "..\\Project2" 
                # Now it should still be "..\\Project2" if it's nested one level.
                
                # Let's do a more intelligent replacement.
                # If project is in root/Subfolder/Project.csproj and refers to root/OtherProject/Other.csproj
                # Original: Include="..\OtherProject\Other.csproj" -> This is STILL CORRECT!
                
                # BUT, if a project was in root (EnbeddedRequirmentToTest.csproj) and referred to "..\DocumentManager"
                # It now should refer to "DocumentManager" (no dots) or ".\DocumentManager"
                
                if root == root_dir:
                     new_csproj = csproj_content.replace(r'Include="..\\', r'Include="')
                     with open(file_path, 'w', encoding='utf-8', newline='') as f:
                         f.write(new_csproj)
                     print(f"Updated references in root project: {file}")
            
print("Refactoring complete.")
