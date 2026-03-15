cd 'C:\Users\yuto\Desktop\GitHub\lifeos'
try {
    & '.\bin\Debug\net8.0-windows\LifeOS.exe'
} catch {
    $_ | Out-File 'crash.txt'
}
