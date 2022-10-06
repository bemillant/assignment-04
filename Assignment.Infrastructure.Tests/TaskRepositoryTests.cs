namespace Assignment.Infrastructure.Tests;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Core;

public class TaskRepositoryTests : IDisposable
{
    private readonly KanbanContext context;
    private readonly TaskRepository taskRep;

    public TaskRepositoryTests()
    {
        var connection = new SqliteConnection("Filename=:memory:");
        connection.Open();
        var builder = new DbContextOptionsBuilder<KanbanContext>();
        builder.UseSqlite(connection);
        var context = new KanbanContext(builder.Options);
        context.Database.EnsureCreated();


        //Tags
        var cleaning = new Tag() { Name = "Cleaning", Id = 1 };
        var urgent = new Tag() { Name = "Urgent", Id = 2 };
        var TBD = new Tag() { Name = "TBD", Id = 3 };
        context.Tags.AddRange(cleaning, urgent, TBD);

        var user1 = new User { Name = "Brian", Id = 1, Email = "br@itu.dk" };
        context.Users.Add(user1);

        //Tasks
        var task1 = new Task() { Title = "Clean Office", Id = 1, State = State.Active };
        var task2 = new Task() { Title = "Do Taxes", Id = 2, State = State.New };
        var task3 = new Task() { Title = "Go For A Run", Id = 3, State = State.Resolved };
        var task4 = new Task() { Title = "Talk with mum", Id = 4, State = State.Resolved, AssignedTo = context.Users.Find(1) };
        context.Tasks.AddRange(task1, task2, task3, task4);


        context.SaveChanges();

        this.context = context;
        taskRep = new TaskRepository(context);

    }

    [Fact]
    public void Create_should_set_New_Created_and_StateUpdated()
    {
        var now = DateTime.UtcNow;
        var (response, taskId) = taskRep.Create(new TaskCreateDTO("test", 1, "test", ArraySegment<string>.Empty));

        response.Should().Be(Response.Created);
        var task = taskRep.Read(taskId);

        task.State.Should().Be(State.New);
        task.Created.Should().BeCloseTo(now, precision: TimeSpan.FromSeconds(5));
        task.StateUpdated.Should().BeCloseTo(now, precision: TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Update_state_sets_StateUpdated()
    {
        var now = DateTime.UtcNow;
        var taskId = 1;

        var response =
            taskRep.Update(new TaskUpdateDTO(taskId, "test", 1, "test", ArraySegment<string>.Empty, State.Closed));

        response.Should().Be(Response.Updated);

        var task = taskRep.Read(taskId);
        task.StateUpdated.Should().BeCloseTo(now, precision: TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_task_should_return_Created()
    {
        var list = new List<String> { "Cleaning", "Urgent" };
        var newTask = new TaskCreateDTO("NewTask", 1, "New Task that is something", list);

        var (response, id) = taskRep.Create(newTask);
        response.Should().Be(Response.Created);

        id.Should().Be(new TaskDTO(5, "NewTask", "Brian", list, State.New).Id);

    }


    [Fact]
    public void Delete_task_that_is_new_should_return_deleted()
    {
        var response = taskRep.Delete(2);
        response.Should().Be(Response.Deleted);
        context.Tasks.Find(2).Should().BeNull();
    }

    [Fact]
    public void Delete_task_that_is_active_should_return_state_removed()
    {
        var response = taskRep.Delete(1);
        context.Tasks.Find(1).State.Should().Be(State.Removed);
    }

    [Fact]
    public void Delete_task_that_is_Resolved_should_return_Conflict()
    {
        var response = taskRep.Delete(3);
        response.Should().Be(Response.Conflict);
        context.Tasks.Find(3).State.Should().Be(State.Resolved);
    }

    [Fact]
    public void Update_task_should_give_updated_tags()
    {

        var list = new List<string> { "Urgent", "TBD" };
        var urgent = new Tag() { Name = "Urgent", Id = 2, Tasks = new List<Task> { context.Tasks.Find(1) } };
        var TBD = new Tag() { Name = "TBD", Id = 3, Tasks = new List<Task> { context.Tasks.Find(1) } };
        var listT = new List<Tag> { urgent, TBD };

        var updateTask = new TaskUpdateDTO(1, "Clean Office", 1, null, list, State.Active);

        var resp = taskRep.Update(updateTask);
        resp.Should().Be(Response.Updated);

        context.Tasks.Find(1).Tags.Count().Should().Be(listT.Count());
    }

    [Fact]
    public void Assign_user_that_does_not_exist_return_BadRequest()
    {
        var updateTask = new TaskUpdateDTO(1, "Clean Office", 100, null, null, State.Active);

        var response = taskRep.Update(updateTask);
        response.Should().Be(Response.BadRequest);

    }

    public void Dispose()
    {
        context.Dispose();
    }


    [Fact]
    public void Read_should_give_correct_task()
    {
        var taskD = new TaskDetailsDTO(taskRep.Read(2).Id, taskRep.Read(2).Title, taskRep.Read(2).Description, taskRep.Read(2).Created, taskRep.Read(2).AssignedToName, taskRep.Read(2).Tags, taskRep.Read(2).State, taskRep.Read(2).StateUpdated);
        var result = taskRep.Read(2);
        result.Should().Be(taskD);
    }

    [Fact]
    public void Read_all_should_give_all_tasks()
    {
        var taskD1 = new TaskDTO(taskRep.Read(1).Id, taskRep.Read(1).Title, taskRep.Read(1).AssignedToName, taskRep.Read(1).Tags, taskRep.Read(1).State);
        var taskD2 = new TaskDTO(taskRep.Read(2).Id, taskRep.Read(2).Title, taskRep.Read(2).AssignedToName, taskRep.Read(1).Tags, taskRep.Read(2).State);
        var taskD3 = new TaskDTO(taskRep.Read(3).Id, taskRep.Read(3).Title, taskRep.Read(3).AssignedToName, taskRep.Read(1).Tags, taskRep.Read(3).State);
        var taskD4 = new TaskDTO(taskRep.Read(4).Id, taskRep.Read(4).Title, taskRep.Read(4).AssignedToName, taskRep.Read(1).Tags, taskRep.Read(4).State);
        var list = new List<TaskDTO>();
        list.Add(taskD1); list.Add(taskD2); list.Add(taskD3); list.Add(taskD4);
        taskRep.ReadAll().Should().BeEquivalentTo(list);
    }


    [Fact]
    public void Read_all_by_state_Should_give_all_tasks_with_state_Active()
    {
        var taskD1 = new TaskDTO(taskRep.Read(1).Id, taskRep.Read(1).Title, taskRep.Read(1).AssignedToName, taskRep.Read(1).Tags, taskRep.Read(1).State);
        var list = new List<TaskDTO>();
        list.Add(taskD1);

        taskRep.ReadAllByState(State.Active).Should().BeEquivalentTo(list);
    }

    [Fact]
    public void Read_all_removed_should_show_empty_list()
    {
        var list = new List<TaskDTO>();
        taskRep.ReadAllRemoved().Should().BeEquivalentTo(list);
    }

    [Fact]
    public void Read_all_by_tag()
    {
        var list = new List<TaskDTO>();
        taskRep.ReadAllByTag("TBD").Should().BeEquivalentTo(list);
    }

    [Fact]
    public void Read_all_by_users()
    {
        var taskD1 = new TaskDTO(taskRep.Read(4).Id, taskRep.Read(4).Title, taskRep.Read(4).AssignedToName, taskRep.Read(4).Tags, taskRep.Read(4).State);
        var list = new List<TaskDTO>();
        list.Add(taskD1);

        taskRep.ReadAllByUser(1).Should().BeEquivalentTo(list);
    }


}





