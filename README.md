# SyncSVN
Doxygen docs: https://biblbrox.github.io/SyncSVN/html/class_repository_lib_1_1_s_v_n_file_repository.html

# Usage example
Init repository and checkout:
```c#
var config = new SVNFileRepositoryConfig();
var repo = new SVNFileRepository(config);
repo.Checkout();
```

Config loaded from App.Settings.

Update file:
```c#
var file = "foo.txt"; // Relative to svn root
repo.Download(file);
```

Pull repo and solve conflicts:
```c#
repo.Pull((List<string> list) => {
/*Resolve conflict and return map with files marked true or false(replace or stay with own file)*/
});
```


## Задачи:
- [x] Коммит директории без конфликтов
- [x] Коммит отдельного файла без конфликтов
- [x] Checkout
- [x] Update директории без конфликтов
- [x] Update отдельного файла без конфликтов
- [x] Удаление файла с записью в историю коммитов
- [x] Конфликт при получении изменений при редактирования локального файла(при измененном в репозитории) 
- [x] Конфликт при обновлении удаленного файла отредактированным локальным(при измененном в репозитории) 
- [x] Конфликт при Update измененного локального файла при удалении файла в репозитории
- [x] Несколько файлов. Удаление файла с записью в историю коммитов
- [x] Несколько файлов. Конфликт при получении изменений при редактирования локального файла(при измененном в репозитории) 
- [x] Несколько файлов. Конфликт при обновлении удаленного файла отредактированным локальным(при измененном в репозитории) 
- [x] Несколько файлов. Конфликт при Update измененного локального файла при удалении файла в репозитории
- [ ] Параллельный доступ к репозиторию(с одного пользователя)

