using System;
using System.Threading.Tasks;
using Pathfinder1eHelper.Models.Combat;

namespace Pathfinder1eHelper.Services;

/// <summary>
/// 后台写回器：把频繁的档案变更合并为“最多一次在途保存”，并在后台线程写盘，
/// 避免每次编辑都在 UI 线程同步写 JSON。最新的档案快照会覆盖尚未写盘的旧快照。
/// </summary>
/// <remarks>
/// 通过 <see cref="Schedule"/> 提交快照，<see cref="FlushAsync"/> 等待全部挂起写入完成
/// （测试与退出前使用）；失败信息记录在 <see cref="Error"/>，不抛出到调用方。
/// </remarks>
public sealed class CharacterAutoSaver(ICharacterRepository repository)
{
    private readonly ICharacterRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    private readonly object _gate = new();

    private CharacterProfile? _pending;
    private bool _saving;
    private Task _loop = Task.CompletedTask;

    /// <summary>最近一次保存失败的消息；成功一次后清空。</summary>
    public string? Error { get; private set; }

    /// <summary>提交一个待写快照；已有保存在途时仅覆盖待写快照。</summary>
    public void Schedule(CharacterProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        lock (_gate)
        {
            _pending = profile;
            if (_saving)
            {
                return;
            }

            _saving = true;
            _loop = Task.Run(SaveLoopAsync);
        }
    }

    /// <summary>等待当前所有挂起写入结束。</summary>
    public async Task FlushAsync()
    {
        while (true)
        {
            Task loop;
            lock (_gate)
            {
                if (!_saving && _pending is null)
                {
                    return;
                }

                loop = _loop;
            }

            await loop.ConfigureAwait(false);
        }
    }

    private async Task SaveLoopAsync()
    {
        while (true)
        {
            CharacterProfile? profile;
            lock (_gate)
            {
                profile = _pending;
                _pending = null;
                if (profile is null)
                {
                    _saving = false;
                    return;
                }
            }

            try
            {
                _repository.Save(profile);
                Error = null;
            }
            catch (Exception ex)
            {
                Error = ex.Message;
            }
        }
    }
}
