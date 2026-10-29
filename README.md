# LocalDrop

Stuur bestanden van je telefoon naar je laptop (of andersom) via het lokale netwerk, zonder WhatsApp of cloud.

**Downloads:** zie [Releases](../../releases) voor `LocalDrop-Setup-<versie>.exe` (Windows) en `LocalDrop-<versie>.apk` (Android, sideload: sta "onbekende bronnen" toe voor je browser of bestandsbeheer).

Eén apparaat draait de LocalDrop-app. Die host een kleine webpagina op het LAN. Elk ander apparaat op dezelfde wifi opent die pagina in de browser (QR-code scannen), vult de 4-cijferige code in en dropt bestanden. Ze komen terecht in `Downloads\LocalDrop\<afzender>\`.

## Platformen

| Platform | Vorm | Status |
|---|---|---|
| Windows 10/11 (x64) | `LocalDrop-Setup-<versie>.exe` (Inno Setup) | gebouwd |
| Android 10+ | `LocalDrop-<versie>.apk` (sideload) | gebouwd |
| macOS / iOS | .NET MAUI ondersteunt het, vereist een Mac met Xcode om te bouwen | niet gebouwd |

## Bouwen

Vereisten op de build-pc: .NET 8 SDK met `maui`-workload, Microsoft OpenJDK 17, Android SDK (platform 36, build-tools 35+), Inno Setup 6.

```powershell
.\build.ps1            # Windows installer + Android APK -> .\dist\
.\build.ps1 -Windows
.\build.ps1 -Android
```

Ontwikkelen op Windows zonder installer:

```powershell
dotnet build LocalDrop\LocalDrop.csproj -f net8.0-windows10.0.19041.0 -t:Run
```

## Hoe het werkt

- **Server**: EmbedIO (managed HTTP-listener, geen adminrechten of URL-ACL nodig) op poort 8787 (valt terug op 8788.. als bezet).
- **Alleen LAN**: verzoeken van adressen buiten RFC1918/link-local/ULA worden met 403 geweigerd.
- **Pincode**: per sessie een willekeurige 4-cijferige code. De QR-code bevat de code al; handmatig intypen kan ook. Na 5 foute pogingen is een IP 60 s geblokkeerd.
- **Upload**: `PUT /upload?name=<bestand>` met headers `X-Pin` en `X-Sender`, body = ruwe bytes. Streamt direct naar schijf (`.part` → definitieve naam), botsende namen krijgen ` (2)`, ` (3)` enz.
- **Windows**: sluiten minimaliseert naar het systeemvak; rechtsklik op het tray-icoon voor Tonen / Map openen / Afsluiten. Toast-melding bij ontvangst met knop "Map openen". Installer biedt "Starten met Windows" (start geminimaliseerd met `--minimized`).
- **Android**: foreground service houdt de server en wifi wakker; bestanden gaan via MediaStore naar `Download/LocalDrop/<afzender>/`, geen opslagpermissie nodig.
- **Taal**: Nederlands als de systeemtaal `nl-*` is, anders Engels. De drop-pagina volgt de browsertaal.

## Structuur

```
LocalDrop/
  Services/DropServer.cs      webserver + pincode + LAN-check
  Services/NetUtil.cs         IP-detectie, LAN-check, naam-sanitizing
  Services/IFileSink.cs       opslag-abstractie + INotifier
  MainViewModel.cs, MainPage.xaml
  Resources/Raw/drop.html     de webpagina die de telefoon ziet
  Platforms/Windows/          tray, toast, Downloads-map, venster verbergen
  Platforms/Android/          MediaStore, notificaties, foreground service
installer/LocalDrop.iss       Inno Setup-script
build.ps1                     bouwt alles naar dist\
```

## Bekende beperkingen

- Geen HTTPS (op een LAN met zelf-ondertekende certificaten geeft dat alleen waarschuwingen). Verkeer blijft op het lokale netwerk.
- De Android-APK wordt ondertekend met een keystore die niet in de repo zit. Kopieer `LocalDrop/Signing.props.example` naar `Signing.props`, maak een keystore met `keytool` en bewaar die: alleen een APK met dezelfde sleutel kan over een bestaande installatie heen worden geïnstalleerd.
- Windows Firewall: de installer voegt een regel toe wanneer hij als beheerder draait. Anders vraagt Windows bij de eerste start; kies dan "Particuliere netwerken".
