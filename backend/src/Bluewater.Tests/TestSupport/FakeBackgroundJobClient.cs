using Hangfire;
using Hangfire.Common;
using Hangfire.States;

namespace Bluewater.Tests.TestSupport;

/// <summary>
/// Hand-rolled fake for <see cref="IBackgroundJobClient"/> — the real Hangfire client needs
/// JobStorage.Current wired to Postgres, which the SQLite test base doesn't have. Records
/// enqueued jobs so tests can assert on what would have been scheduled.
/// </summary>
public class FakeBackgroundJobClient : IBackgroundJobClient
{
    public List<RecordedJob> EnqueuedJobs { get; } = [];

    public string Create(Job job, IState state)
    {
        EnqueuedJobs.Add(new RecordedJob(job.Type, job.Method.Name, job.Args.ToList()));
        return Guid.NewGuid().ToString();
    }

    public bool ChangeState(string jobId, IState state, string? expectedState) => true;

    public record RecordedJob(Type Type, string MethodName, List<object?> Args);
}
