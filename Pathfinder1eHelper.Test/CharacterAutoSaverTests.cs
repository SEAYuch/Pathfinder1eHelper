using System.IO;
using Pathfinder1eHelper.Models.Combat;
using Pathfinder1eHelper.Services;

namespace Pathfinder1eHelper.Test;

/// <summary>后台写回器：合并快照、等待落盘与失败捕获。</summary>
public class CharacterAutoSaverTests
{
    [Fact]
    public async Task FlushAsync_persists_the_latest_scheduled_snapshot()
    {
        var repository = new FakeCharacterRepository();
        var saver = new CharacterAutoSaver(repository);

        saver.Schedule(new CharacterProfile { Name = "一", Level = 1 });
        saver.Schedule(new CharacterProfile { Name = "二", Level = 2 });

        await saver.FlushAsync();

        Assert.NotNull(repository.LastSaved);
        Assert.Equal("二", repository.LastSaved!.Name);
        Assert.Null(saver.Error);
    }

    [Fact]
    public async Task FlushAsync_without_pending_save_is_a_noop()
    {
        var saver = new CharacterAutoSaver(new FakeCharacterRepository());

        await saver.FlushAsync();

        Assert.Null(saver.Error);
    }

    [Fact]
    public async Task Save_failure_is_captured_without_throwing()
    {
        var saver = new CharacterAutoSaver(new ThrowingCharacterRepository());

        saver.Schedule(new CharacterProfile { Name = "x" });
        await saver.FlushAsync();

        Assert.NotNull(saver.Error);
    }

    private sealed class ThrowingCharacterRepository : ICharacterRepository
    {
        public string DirectoryPath => "(throwing)";

        public IReadOnlyList<CharacterProfile> LoadAll() => [];

        public void Save(CharacterProfile profile) => throw new IOException("disk full");

        public void Delete(Guid id)
        {
        }
    }
}
