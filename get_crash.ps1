cd 'C:\Users\yuto\Desktop\GitHub\lifeos'
$result = dotnet run 2>&1
$result | Set-Content 'crash_log.txt' -Encoding UTF8
