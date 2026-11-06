using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Yamori.Accounts;
using Yamori.Core.Settings;
using Yamori.Core.Storage;
using Yamori.Localization;
using Yamori.Presentation;
using Yamori.Services;

namespace Yamori.ViewModels;

/// <summary>
/// 設定画面の状態。記録の保存先と表示言語を変更する。
/// </summary>
public sealed class SettingsViewModel : ObservableObject
{
    private readonly ISettingsStore _settingsStore;
    private readonly HistoryStorageService _storage;
    private readonly IFolderPicker _folderPicker;
    private readonly LanguageService _languageService;

    private YamoriSettings _settings;
    private int _storageIndex;
    private bool _isApplying;
    private string _storageErrorText = string.Empty;

    /// <summary>
    /// 必要な処理と今の設定を指定して作る。
    /// </summary>
    /// <param name="settingsStore">設定の保存先。</param>
    /// <param name="settings">今の設定。</param>
    /// <param name="storage">記録の保存先を切り替える処理。</param>
    /// <param name="oneDriveLocator">OneDrive フォルダーを探す処理。</param>
    /// <param name="folderPicker">フォルダーを選んでもらう処理。</param>
    /// <param name="languageService">表示言語を切り替える処理。</param>
    /// <param name="account">サインイン中のアカウント。</param>
    /// <exception cref="ArgumentNullException">いずれかの引数が null。</exception>
    public SettingsViewModel(
        ISettingsStore settingsStore,
        YamoriSettings settings,
        HistoryStorageService storage,
        IOneDriveLocator oneDriveLocator,
        IFolderPicker folderPicker,
        LanguageService languageService,
        WindowsAccount account)
    {
        ArgumentNullException.ThrowIfNull(settingsStore);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(storage);
        ArgumentNullException.ThrowIfNull(oneDriveLocator);
        ArgumentNullException.ThrowIfNull(folderPicker);
        ArgumentNullException.ThrowIfNull(languageService);
        ArgumentNullException.ThrowIfNull(account);

        _settingsStore = settingsStore;
        _settings = settings;
        _storage = storage;
        _folderPicker = folderPicker;
        _languageService = languageService;
        _storageIndex = (int)settings.StorageLocation;

        Account = account;
        OneDriveRoot = oneDriveLocator.FindRoot();
        Languages =
        [
            new(AppLanguage.System, UiText.Get("Settings_LanguageSystem")),
            new(AppLanguage.Japanese, "日本語"),
            new(AppLanguage.English, "English"),
            new(AppLanguage.Spanish, "Español"),
        ];
        ChooseFolderCommand = new AsyncRelayCommand(ChooseFolderAsync, () => !IsApplying);
    }

    /// <summary>
    /// 表示言語が変わったときに発生する。画面を作り直す合図。
    /// </summary>
    public event EventHandler? LanguageChanged;

    /// <summary>サインイン中のアカウントを取得する。</summary>
    public WindowsAccount Account { get; }

    /// <summary>OneDrive フォルダー。見つからなければ null。</summary>
    public string? OneDriveRoot { get; }

    /// <summary>OneDrive を保存先に選べるかどうかを取得する。</summary>
    public bool IsOneDriveAvailable => OneDriveRoot is not null;

    /// <summary>OneDrive の選択肢に添える説明を取得する。</summary>
    public string OneDriveNoteText => UiText.Get(IsOneDriveAvailable ? "Settings_OneDriveNote" : "Settings_OneDriveNotFound");

    /// <summary>言語の選択肢を取得する。</summary>
    public IReadOnlyList<LanguageOption> Languages { get; }

    /// <summary>フォルダーを選び直すコマンドを取得する。</summary>
    public IAsyncRelayCommand ChooseFolderCommand { get; }

    /// <summary>
    /// 選ばれている言語の、<see cref="Languages"/> の中での位置を取得または設定する。変えるとすぐに保存し、画面を作り直す。
    /// </summary>
    public int SelectedLanguageIndex
    {
        get => Languages.ToList().FindIndex(o => o.Language == _settings.Language);
        set
        {
            if (value < 0 || value >= Languages.Count || Languages[value].Language == _settings.Language)
            {
                return;
            }

            var language = Languages[value].Language;
            Save(_settings with { Language = language });
            _languageService.Apply(language);
            OnPropertyChanged();
            LanguageChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// 選ばれている保存先の番号（<see cref="StorageLocationKind"/> の値）を取得または設定する。変えるとすぐに切り替える。
    /// </summary>
    public int StorageIndex
    {
        get => _storageIndex;
        set
        {
            if (value < 0 || !SetProperty(ref _storageIndex, value))
            {
                return;
            }

            _ = ChangeStorageAsync((StorageLocationKind)value);
        }
    }

    /// <summary>指定フォルダーの表示を取得する。</summary>
    public string CustomFolderText => _settings.CustomFolderPath ?? UiText.Get("Settings_NoFolderChosen");

    /// <summary>今の保存先のフォルダーを示す文字列を取得する。</summary>
    public string CurrentPathText => StorageLocationTexts.PathLabel(_storage.CurrentLocation);

    /// <summary>選んだ保存先が使えないときの説明を取得する。</summary>
    public string FallbackText => StorageLocationTexts.FallbackNote(_storage.CurrentLocation);

    /// <summary>選んだ保存先が使えず、この PC の中に保存しているかどうかを取得する。</summary>
    public bool HasFallback => _storage.CurrentLocation.IsFallback;

    /// <summary>保存先を変えられなかったときの説明を取得する。</summary>
    public string StorageErrorText
    {
        get => _storageErrorText;
        private set
        {
            if (SetProperty(ref _storageErrorText, value))
            {
                OnPropertyChanged(nameof(HasStorageError));
            }
        }
    }

    /// <summary>保存先を変えられなかったかどうかを取得する。</summary>
    public bool HasStorageError => StorageErrorText.Length > 0;

    /// <summary>保存先を切り替えている最中かどうかを取得する。</summary>
    public bool IsApplying
    {
        get => _isApplying;
        private set
        {
            if (SetProperty(ref _isApplying, value))
            {
                OnPropertyChanged(nameof(CanChangeStorage));
                ChooseFolderCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>保存先を変更できる状態かどうか（切り替え中でないか）を取得する。</summary>
    public bool CanChangeStorage => !IsApplying;

    private async Task ChangeStorageAsync(StorageLocationKind kind)
    {
        var folder = _settings.CustomFolderPath;
        if (kind == StorageLocationKind.CustomFolder && folder is null)
        {
            folder = await _folderPicker.PickFolderAsync();
            if (folder is null)
            {
                RevertStorageIndex();
                return;
            }
        }

        await ApplyStorageAsync(_settings with { StorageLocation = kind, CustomFolderPath = folder });
    }

    private async Task ChooseFolderAsync()
    {
        var folder = await _folderPicker.PickFolderAsync();
        if (folder is null)
        {
            return;
        }

        _storageIndex = (int)StorageLocationKind.CustomFolder;
        OnPropertyChanged(nameof(StorageIndex));
        await ApplyStorageAsync(_settings with { StorageLocation = StorageLocationKind.CustomFolder, CustomFolderPath = folder });
    }

    private async Task ApplyStorageAsync(YamoriSettings next)
    {
        IsApplying = true;
        StorageErrorText = string.Empty;
        try
        {
            // 履歴のコピーは時間がかかることがあるため、UI スレッドを止めない。
            await Task.Run(() => _storage.Apply(next));
            Save(next);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            StorageErrorText = UiText.Format("Settings_StorageError", ex.Message);
            RevertStorageIndex();
        }
        finally
        {
            IsApplying = false;
            OnPropertyChanged(nameof(CustomFolderText));
            OnPropertyChanged(nameof(CurrentPathText));
            OnPropertyChanged(nameof(FallbackText));
            OnPropertyChanged(nameof(HasFallback));
        }
    }

    private void RevertStorageIndex()
    {
        _storageIndex = (int)_settings.StorageLocation;
        OnPropertyChanged(nameof(StorageIndex));
    }

    private void Save(YamoriSettings next)
    {
        _settings = next;
        try
        {
            _settingsStore.Save(next);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // 設定を保存できなくても、今回の起動中は新しい設定で動かす。
            StorageErrorText = UiText.Format("Settings_StorageError", ex.Message);
        }
    }
}
