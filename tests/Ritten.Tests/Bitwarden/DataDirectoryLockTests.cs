using Ritten.Bitwarden;

namespace Ritten.Tests.Bitwarden;

public class DataDirectoryLockTests
{
    [Fact]
    public async Task Acquire_MakesASecondRunWaitItsTurn()
    {
        var directory = Directory.CreateTempSubdirectory().FullName;
        try
        {
            using var first = await DataDirectoryLock.Acquire(directory, TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);

            var failure = await Should.ThrowAsync<TimeoutException>(
                () => DataDirectoryLock.Acquire(directory, TimeSpan.FromMilliseconds(300), TestContext.Current.CancellationToken));

            failure.Message.ShouldContain(directory);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Acquire_HasTheDirectoryOnceTheRunBeforeHasFinished()
    {
        var directory = Directory.CreateTempSubdirectory().FullName;
        try
        {
            var first = await DataDirectoryLock.Acquire(directory, TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);
            var second = DataDirectoryLock.Acquire(directory, TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

            first.Dispose();

            using var acquired = await second;
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
