# OLA3D Calculator 1.0

CAD-tervezési és többanyagú 3D nyomtatási árkalkulátor Windowsra. **Szerző: OLA3D.**

[English documentation](README.md)

## Letöltés és telepítés

A tároló **Releases** oldaláról töltsd le az `OLA3D-Calculator-1.0.0-Setup-win-x64.exe` fájlt. Indítsd el, válassz **Magyar** vagy **English** nyelvet, majd HUF, EUR vagy USD pénznemet, és igény szerint jelöld be az **Asztali parancsikon létrehozása** lehetőséget. A program az EXE közvetlen indításakor is a telepítésnél választott nyelvet használja. A nyelv vagy pénznem megváltoztatásához futtasd újra a telepítőt.

Windows 10/11, 64 bites rendszer. Külön .NET-telepítés és rendszergazdai jogosultság nem szükséges. A futtatókörnyezet a csomag része. Ez a kiadás nincs digitálisan aláírva.

## Használat

- **Tervezés:** összetettség, pontosság, szkennelés, módosítások, határidő és kiszállás alapján számított ajánlat.
- **Nyomtatás:** több anyag, tömeg, gépidő, veszteség, minimumdíj és kerekítés.
- **Beállítások:** szabadon módosítható díjak, szorzók és anyagkatalógus.
- **Pénznemválasztás telepítéskor:** HUF, EUR és USD, pénznemenként külön mentett díjszabással.
- **Alap CAD-óradíjak:** 5000 Ft, 13 EUR és 15 USD. Módosítható mintadíjak, nem élő árfolyamok. Minden pénznemhez tartoznak további alapdíjak és anyagárak is.
- A program használata közben a pénznem rögzített. Másik pénznem választásához futtasd újra a telepítőt.
- A **Gyári alaphelyzet** csak az aktuális pénznem díjait és katalógusát állítja vissza.

## Beállításfájlok

Helyük: `%APPDATA%\OLA3D Calculator`. A `settings-HUF.json`, `settings-EUR.json` és `settings-USD.json` a pénznemenként különálló díjakat tárolja. A nyelv és pénznem a telepítési mappában található `language.ini` fájlban szerepel.

A csomag nem tartalmaz személyes beállításokat, és más alkalmazások korábbi beállításait sem importálja. Eltávolításkor az egyéni díjszabások megmaradnak.

A nyomtatási díj képlete: anyagonként `(tömeg / 1000) × kg-ár × anyagszorzó × veszteségi szorzó`, majd ehhez hozzáadódik a `gépidő × gépóradíj`. Végül a minimumdíj és a kerekítés érvényesül. Pénznemváltáskor másik díjlista töltődik be, nem élő devizaátváltás történik.

## Fordítás

A .NET 8 SDK (vagy kompatibilis újabb SDK) és az [Inno Setup 6](https://jrsoftware.org/isdl.php) szükséges. A fordítás és a tesztelés lépései az [angol leírásban](README.md#build) találhatók.

Verzió: **1.0.0**. Copyright © 2026 OLA3D. Ehhez a kiadáshoz nincs nyílt forráskódú licenc megadva.
