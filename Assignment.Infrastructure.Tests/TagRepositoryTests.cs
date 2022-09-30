namespace Assignment.Infrastructure.Tests;
using Assignment.Core;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;


public class TagRepositoryTests : IDisposable
{
    private readonly KanbanContext _context;
    private readonly TagRepository _tagRepository;

    public TagRepositoryTests()
    {
        var connection = new SqliteConnection("Filename=:memory:");
        connection.Open();
        var builder = new DbContextOptionsBuilder<KanbanContext>();
        builder.UseSqlite(connection);
        var context = new KanbanContext(builder.Options);
        context.Database.EnsureCreated();

        context.Tags.AddRange(
            new Tag { Name = "Cleaning", Id = 1 },
            new Tag { Name = "Urgent", Id = 2 },
            new Tag { Name = "TBD", Id = 3 });
        context.SaveChanges();

        _context = context;
        _tagRepository = new TagRepository(context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public void Create_Tag_Should_Give_Tag()
    {
        var (response, id) = _tagRepository.Create(new TagCreateDTO("HighPrio"));

        response.Should().Be(Response.Created);

        id.Should().Be(new TagDTO(4, "HighPrio").Id);
    }

    [Fact]
    public void Create_Tag_Should_give_conflict_since_Tag_exists()
    {
        var (response, id) = _tagRepository.Create(new TagCreateDTO("Cleaning"));

        response.Should().Be(Response.Conflict);

        id.Should().Be(new TagDTO(1, "Cleaning").Id);
    }

    [Fact]
    public void Delete_existing_tag_not_in_use()
    {
        var response = _tagRepository.Delete(1);
        response.Should().Be(Response.Deleted);
        _context.Tags.Find(1).Should().BeNull();
    }

    [Fact]
    public void Delete_non_existing_tag_return_notFound()
    {
        var response = _tagRepository.Delete(100);
        response.Should().Be(Response.NotFound);
        _context.Tags.Find(100).Should().BeNull();
    }

    [Fact]
    public void Delete_tag_in_use_without_using_force_should_give_conflict()
    {
        var task1 = new Task { Title = "Clean Office", Id = 1, State = State.Active };
        var task2 = new Task { Title = "Do Taxes", Id = 2, State = State.New };
        var list = new List<Task> { task1, task2 };
        _context.Tags.Find(1)!.Tasks = list;

        var response = _tagRepository.Delete(1);
        response.Should().Be(Response.Conflict);
        _context.Tags.Find(1).Should().NotBeNull();
    }

    [Fact]
    public void Read_return_the_right_tag()
    {
        var tagD = new TagDTO(1, "Cleaning");
        var result = _tagRepository.Read(1);
        result.Should().Be(tagD);
    }

    [Fact]
    public void ReadAll_Should_return_all_the_tags()
    {
        var t1 = new TagDTO(1, "Cleaning");
        var t2 = new TagDTO(2, "Urgent");
        var t3 = new TagDTO(3, "TBD");
        var listOfTags = new List<TagDTO> { t1, t2, t3 };
        var result = _tagRepository.ReadAll();

        result.Should().BeEquivalentTo(listOfTags);
    }

    [Fact]
    public void Update_tag_should_give_updated()
    {
        var response = _tagRepository.Update(new TagUpdateDTO(1, "Office work"));
        response.Should().Be(Response.Updated);

        var entity = _context.Tags.Find(1)!;
        entity.Name.Should().Be("Office work");
    }
}