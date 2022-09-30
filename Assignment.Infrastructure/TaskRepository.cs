namespace Assignment.Infrastructure;

public class TaskRepository : ITaskRepository
{
    KanbanContext context;

    public TaskRepository(KanbanContext context)
    {
        this.context = context;
    }

    public (Response Response, int TaskId) Create(TaskCreateDTO task)
    {
        var entity = context.Tasks.FirstOrDefault(c => c.Title == task.Title);
        Response response;

        if (entity is null)
        {
            entity = new Task { Title = task.Title, State = State.New, Created = DateTime.UtcNow, AssignedTo = null, StateUpdated = DateTime.UtcNow };

            context.Tasks.Add(entity);
            context.SaveChanges();

            response = Response.Created;
        }
        else
        {
            response = Response.Conflict;
        }
        return (response, entity.Id);
    }

    public Response Delete(int taskId)
    {
        var entity = context.Tasks.FirstOrDefault(c => c.Id == taskId);

        switch (entity?.State)
        {
            case State.Active:
                entity.State = State.Removed;
                context.SaveChanges();
                return Response.Updated;
            case State.Resolved:
            case State.Closed:
            case State.Removed:
                return Response.Conflict;
            case State.New:
                context.Tasks.Remove(entity);
                context.SaveChanges();
                return Response.Deleted;
            case null:
                return Response.NotFound;
            default:
                throw new NotImplementedException("State variant not implemented");
        }
    }

    public TaskDetailsDTO Read(int taskId)
    {
        if (context.Tasks.Find(taskId) is not null)
        {
            var Id = context.Tasks.Find(taskId).Id;
            var Title = context.Tasks.Find(taskId).Title;
            var Desc = context.Tasks.Find(taskId).Description;
            var Created = context.Tasks.Find(taskId).Created;
            string Name = null;
            if (context.Tasks.Find(taskId).AssignedTo is not null)
            {
                Name = context.Tasks.Find(taskId).AssignedTo.Name;
            }
            List<string> list = null;
            if (context.Tasks.Find(taskId).Tags is not null)
            {
                list = context.Tasks.Find(taskId).Tags.Select(t => t.Name).ToList();
            }
            var State = context.Tasks.Find(taskId).State;
            var StateUp = context.Tasks.Find(taskId).StateUpdated;

            return new TaskDetailsDTO(Id, Title, Desc, Created, Name, list, State, StateUp);
        }
        else
        {
            return null;
        }
    }

    public IReadOnlyCollection<TaskDTO> ReadAll()
    {
        var list = new List<TaskDTO>();
        foreach (var task in context.Tasks)
        {
            list.Add(new TaskDTO(task.Id, task.Title, task.AssignedTo.Name, task.Tags.Select(t => t.Name).ToList(), task.State));
        }
        return list;
    }

    public IReadOnlyCollection<TaskDTO> ReadAllByState(State State)
    {
        var list = new List<TaskDTO>();
        foreach (var task in context.Tasks)
        {
            if (task.State == State)
            {
                list.Add(new TaskDTO(task.Id, task.Title, task.AssignedTo.Name, task.Tags.Select(t => t.Name).ToList(), task.State));
            }
        }
        return list;

    }

    public IReadOnlyCollection<TaskDTO> ReadAllByTag(string tag)
    {
        var list = new List<TaskDTO>();
        foreach (var task in context.Tasks)
        {
            foreach (var t in task.Tags)
            {
                if (t.Name.Equals(tag))
                {
                    list.Add(new TaskDTO(task.Id, task.Title, task.AssignedTo.Name, task.Tags.Select(t => t.Name).ToList(), task.State));
                }
            }
        }
        return list;
    }

    public IReadOnlyCollection<TaskDTO> ReadAllByUser(int userId)
    {
        var list = new List<TaskDTO>();
        foreach (var task in context.Tasks)
        {
            if (task.AssignedTo.Id == userId)
            {
                list.Add(new TaskDTO(task.Id, task.Title, task.AssignedTo.Name, task.Tags.Select(t => t.Name).ToList(), task.State));
            }
        }
        return list;
    }

    public IReadOnlyCollection<TaskDTO> ReadAllRemoved()
    {
        var list = new List<TaskDTO>();
        foreach (var task in context.Tasks)
        {
            if (task.State == State.Removed)
            {
                list.Add(new TaskDTO(task.Id, task.Title, task.AssignedTo.Name, task.Tags.Select(t => t.Name).ToList(), task.State));
            }
        }
        return list;
    }

    public Response Update(TaskUpdateDTO task)
    {
        var entity = context.Tasks.Find(task.Id);
        Response response;

        if (entity is null)
        {
            response = Response.NotFound;
        }
        //if two tasks exists with the same titles but different ids
        else if (context.Tasks.FirstOrDefault(t => t.Id != task.Id && t.Title == task.Title) != null)
        {
            response = Response.Conflict;
        }
        else if (context.Users.Find(task.AssignedToId) is null)
        {
            response = Response.BadRequest;
        }
        else
        {
            entity.AssignedTo = task.AssignedToId is not null ? context.Users.Find(task.AssignedToId) : entity.AssignedTo;
            entity.Description = task.Description is not null ? task.Description : entity.Description;


            if (task.Tags is not null)
            {
                entity.Tags = new List<Tag>();
                foreach (var tag in task.Tags!)
                {
                    foreach (var conTag in context.Tags)
                    {
                        if (conTag.Name == tag)
                        {
                            entity.Tags.Add(context.Tags.Find(conTag.Id));
                        }
                    }
                }
            }

            entity.State = task.State;
            entity.Title = task.Title;
            entity.StateUpdated = DateTime.UtcNow;
            context.SaveChanges();
            response = Response.Updated;
        }

        return response;

    }
}
