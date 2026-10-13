var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Run();

public class WorkItem
{
    public string State { get; set; }

    public string Area { get; set; }

    public string IterationPath { get; set; }

    public int Priority { get; set; }

    //Epic
    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    //Issue
    public decimal Effort { get; set; }

    //Task
    public string Activity { get; set; }

    public decimal RemainingWork { get; set; }
}
