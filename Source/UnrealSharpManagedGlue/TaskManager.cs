using System.Collections.Generic;
using System.Threading.Tasks;
using EpicGames.UHT.Utils;
using UnrealSharpManagedGlue.Utilities;

namespace UnrealSharpManagedGlue;

public class TaskManager
{
	private readonly List<Task> Tasks = new();

	public void StartTask(UhtExportTaskDelegate action)
	{
		Task? task = SessionStatic.Generator.Factory.CreateTask(action);
		
		if (task == null)
		{
			// Task execution was done synchronously
			return;
		}
		
		Tasks.Add(task);
	}
	
	public void WaitForTasks()
	{
		Task.WaitAll(Tasks.ToArray());
	}
}