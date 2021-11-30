using Microsoft.EntityFrameworkCore;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Autofac;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.Modeling;
using Volo.Abp.EntityFrameworkCore.Sqlite;
using Volo.Abp.Modularity;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Host.AddAppSettingsSecretsJson()
    .UseAutofac();
builder.Services.ReplaceConfiguration(builder.Configuration);
builder.Services.AddApplication<MinimalModule>();
var app = builder.Build();

// app.MapGet("/book", async (context) =>
// {
//     var repository = context.RequestServices.GetRequiredService<BookRepository>();
//     await repository.GetListAsync();
// });

app.MapPost("/book", async (MyDbContext context, string name) =>
{
    var newBook = context.Books.Add(new Book(name));
    await context.SaveChangesAsync();
    return Results.Created($"/book/{newBook.Entity.Id}", newBook.Entity);
});

app.InitializeApplication();
app.Run();

public class Book : AuditedAggregateRoot<Guid>
{
    public Book(string name)
    {
        this.Name = name;
    }
    public string Name { get; set; }
}


public class MyDbContext : AbpDbContext<MyDbContext>
{
    public DbSet<Book> Books => Set<Book>();

    public MyDbContext(DbContextOptions<MyDbContext> options)
        : base(options)
    {

    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.UseSqlite();
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        //Always call the base method
        base.OnModelCreating(builder);

        builder.Entity<Book>(b =>
        {
            b.ToTable("Books");

            //Configure the base properties
            b.ConfigureByConvention();

            //Configure other properties (if you are using the fluent API)
            b.Property(x => x.Name).IsRequired().HasMaxLength(128);
        });
    }
}

public class BookRepository : EfCoreRepository<MyDbContext, Book, Guid>
{
    public BookRepository(IDbContextProvider<MyDbContext> dbContextProvider)
        : base(dbContextProvider)
    {
    }
}

[DependsOn(
    typeof(AbpAspNetCoreMvcModule),
    typeof(AbpAutofacModule),
    typeof(AbpEntityFrameworkCoreSqliteModule)
)]
public class MinimalModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddEndpointsApiExplorer();
        context.Services.AddSwaggerGen();
        context.Services.AddAbpDbContext<MyDbContext>(options =>
        {
            options.AddDefaultRepositories(includeAllEntities: true);
            options.AddRepository<Book, BookRepository>();
        });


        Configure<AbpDbContextOptions>(options =>
        {
            options.UseSqlite();
        });
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();
        var env = context.GetEnvironment();
        // Configure the HTTP request pipeline.
        if (env.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
    }
}