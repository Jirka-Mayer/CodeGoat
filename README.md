Code goat
=========

> Zápočtový program na .NET I, letní semestr 2019, Jiří Mayer

> Webový textový editor umožňující současnou editaci více klienty

Specifikace zadání: [specification/2019-06-12.md](specification/2019-06-12.md)


## Instalace a spouštění

Nejprve je třeba naklonovat repozitář, ale jelikož obsahuje *submodules*, tak je třeba hned poté naklonovat i je:

    git clone git@github.com:Jirka-Mayer/CodeGoat.git CodeGoat
    cd CodeGoat
    git submodule update --init --recursive

Potom je třeba zkompilovat serverový kód. Stačí otevřít `.sln` ve Visual Studiu a sputit *build*.

Nakonec se musí zkompilovat kód klienta, ale na to musíme být ve složce `Client`:

    cd Client

Ale před kompilováním musíme nainstalovat všechny balíčky:

    npm install

A teď můžeme zkompilovat:

    npm run prod

Tím se nám do složky `Server/bin/Debug` vytvoří všechny potřebné soubory a potom stačí spustit server (buď z Visual Studia nebo `mono Server.exe`) a připojit se prohlížečem na adresu `localhost:8080`.


## Přehed aplikace (z pohledu uživatele)

Na hlavní stránce `localhost:8080` lze vstoupit do nějaké místnosti. Místnost odpovídá jednomu textovému souboru a v každé místnosti může být libovolný počet klientů. Místnost se vytvoří automaticky, jakmile do ní vstoupí první klient. Místnost je identifikovaná nějakým textovým řetězcem, který je součástí URL adresy místnosti:

    localhost:8080/room/my-room-identifier

Uvnitř místnosti si klient může zvolit jméno, které uvidí ostatní klienti a server mu přidělí barvu kurzoru (svoji barvu nevidí, ale ostatních ano).

Samotná editace textu je intuitivní, probíhá stejně jako v každém jiném textovém editoru.


## Dokumentace (z pohledu programátora)

Program začíná v souboru `Program.cs` v metodě `MainClass.Main(...)`. Zde se spustí servery a začne se čekat na ukončení aplikace. V okamžiku ukončení (napsání příkazu `exit`) se zastaví všechny servery a proces skončí.

Hlavní dvě části aplikace je HTTP server a WebSocket server.


### HTTP server

HTTP server je reprezentovaný třídou `HttpServer`. Má registrovaných několik cest (route), které se registrují v metodě `MainClass.RegisterHttpServerRoutes`. Každá cesta porovná dotazované URL s regulárním výrazem a když uspěje, předá dotaz příslušnému handleru (lambda funkci). Handler vrátí nějaká data a typ dat a server je odešle klientovi.

Jediné co server dělá je, že odesílá html, css a js soubory ve složce aplikace s minimálními změnami. Veškerá zajímavá logika je v metodě `MainClass.RegisterHttpServerRoutes`, třída `HttpServer` je prostě klasický, jednoduchý http server.


### Web socket server

Web socket server je třída `WebSocketServer` z knihovny Fleck. Ta zajišťuje připojování klientů, příjmání zpráv a odesílání zpráv na klienty. Jedno spojení (klient) je reprezentováno rozhraním `IWebSocketConnection` knihovny Fleck. Já jsem navíc každé spojení zabalil do třídy `Client`, která drží další data o klientovi, jako jeho jméno, barvu a místnost ve které se nachází.


### Editor server

Třída `EditorServer` je místo, kde se řeší zajímavá logika aplikace (synchronizace stavu a kurzoru). Její metoda `HandleNewConnection` je vstupní bod. Je zavolána pro každého nově připojeného klienta. U čerstvého klienta ještě nevíme, do jaké místnosti patří, takže ho jen zařadíme do seznamu klientů a budeme čekat, až se ozve.

Zpracování zpráv od klienta ale neřeší třída `EditorServer`, ale třída `Client` samotná. `EditorServer` pouze klienta vytvoří a registruje mu všechny handlery.

Editor server ještě navíc spravuje seznam místností `Room`. Metoda `ResolveRoom(string identifier)` vytvoří nebo vrátí instanci místnosti.

Poslední kompetence editor serveru je spouštět tzv. *document broadcasty*, což je rozeslání aktuálního stavu dokumentu všem klientům. Editor server ale pouze zavolá na všech místnostech metodu `BroadcastDocumentState`. To se děje každých 30 sekund.


### Klient

Třída `Client` drží data o klientovi, ale hlavně má na starost zpracovávat zprávy, které od klienta dorazí.

Ve chvíli, kdy je klient čertvě připojený, není v žádné místnosti. V takový okamžik může přijmout pouze zprávu typu `join-room`. V té o sobě prohlásí, v jaké místnosti je a logika uvnitř třídy `Client` ho přiřadí do místnosti.

Jakmile je v místnosti, tak jsou všechny zprávy od něho směřovány do místnosti na metodu `Room.OnClientSentMessage(...)`.


### Místnost

Místnost nejprve řeší připojené nového klienta. To iniciuje klient zavoláním metody `Room.OnClientJoined`. Metoda si ho zařadí do seznamu klientů, odešle mu momentální stav dokumentu, odešle mu seznam všech ostatních klientů a všem ostatním klientům ohlásí, že se připojil nový klient.

Teď jsme v konzistetním stavu, kdy máme všechny klienty připojené a místnost vytvořenou.

Nyní se vše zajímavé odehrává v metodě `Room.OnClientSentMessage(...)`. Když dorazí zpráva o změně obsahu, tak se aplikuje na serverový dokument (`Document`) a rozešle se ostatním klientům. Když přijde zpráva o změně pozice kurzoru, tak se jen rozešle ostatním. Podobně se rozesílá změna jména klienta.

Poslední zajímavá zpráva je požadavek o broadcast. Ten klient odešle když má podezření, že jeho lokální dokument se liší od toho na serveru. A jelikož server má vždy pravdu, rozešle svoji pravdu mezi klienty a ti ji přijmou za svou.

Nakonec místnost řeší odpojení klienta. To způsobí zavolání metody `Room.OnClientLeft`. Klient bude odebrán ze seznamu a všem zbývajícím klientům se ohlásí, že se odpojil, aby si ho i oni mohli odebrat z vlastního seznamu klientů.


### Komunikační protokol

Každá jedna zpráva poslaná po web socket spojení je JSON object, který musí mít položku `type`. Tahle položka (nečekaně) určuje typ zprávy. Všechny ostatní položky závisí na typu zprávy.


#### Client `-->` Server


**join-room**

```js
{
    "type": "join-room",
    "room": "string-room-identifier-from-the-url"
}
```

Klient se právě připojil a tohle je jeho první zpráva. Ohlašuje, že je v té a té místnosti.


**change**

```js
{
    "type": "change",
    "change": {...} // change object returned by codemirror with an "id" field added
}
```

Klient u sebe provedl nějakou editaci.


**selection-change**

```js
{
    "type": "selection-change",
    "selection": {...}         // selection object returned by codemirror
}
```

Klient posunul s kurzorem nebo změnil výběr.


**name-changed**

```js
{
    "type": "name-changed",
    "name": "John Doe"
}
```

Klient si změnil jméno. Zpráva přijde také ihned po připojení do místnosti, aby ostatní klienti věděli, jak se klient jmenuje.


**request-document-broadcast**

```js
{
    "type": "request-document-broadcast"
}
```

Klient má podezření, že jeho dokument se neshoduje s tím na serveru, takže žádá o broadcast současného stavu dokumentu.


#### Client `<--` Server


**document-state**

```js
{
    "type": "document-state",
    "document": "Lorem ipsum\nDolor sit amet.",
    "initial": false
}
```

Server posílá klientovi současný stav dokumentu. `initial` je `true`, pokud se jedná o stav, který server posílá čerstvě připojenému klientovi. V takovém případě musí klient přijmout dokument za svůj, vymazat historii a posunout kurzor na začátek. Pokud zpráva není *initial*, tak se jedná o *document state broadcast* a potom klient stav pouze porovná se svým současným stavem a přijme ho jen pokud se liší (a vypíše warning do konzole).


**change-broadcast**

```js
{
    "type": "change-broadcast",
    "change": {...} // change object returned by codemirror with an "id" field added
}
```

Nějaký klient provedl změnu a server ji rozesílá ostatním. Pokud ID změny odpovídá nejstarší spekulativní změně,
tak se změna na klientovi neprovede, protože ji klient už spekulativně provedl. Jen se odstraní ze seznamu spekulativních.


**selection-broadcast**

```js
{
    "type": "selection-broadcast",
    "selection": {...}, // selection object returned by codemirror
    "clientId": 42
}
```

Nějaký klient změnil svůj výběr a server nám to oznamuje. Klient nikdy nedostane svoje vlastní změny, pouze změny ostatních klientů.


**client-update**

```js
{
    "type": "client-update",
    "clientId": 42,
    "name": "John doe",
    "color": "#123456"
}
```

Server nám oznamuje, že došlo ke změně údajů nějakého klienta (nebo se připojil nový klient). (nebo jsme se připojili my a server nám oznamuje, jací ostatní klienti tu jsou).

Tzn. pokud klienta znám, tak si ho aktualizuju a pokud ho neznám, tak si ho přidám do seznamu.


**client-left**

```js
{
    "type": "client-left",
    "clientId": 42
}
```

Nějaký klient se odpojil, můžeme na něho zapomenout.


### Spekulativní změny

Server si drží stav dokumentu a ten je z definice pravdivý. Klienti posílají serveru změny, které provádějí a server je aplikuje na svůj dokument a rozesílá ostatním.

Jenže když klient provede změnu, uživatel ji musí vidět okamžitě. Takže čerstvě provedená změna je spekulativní. Klient si ji pamatuje, dokud ji server nepotvrdí. Protože jediná pravdivá věc je proud změn, který nám posílá server.

Co když ale provedeme spekulativní změnu A a server nám řekne, že někdo jiný provedl změnu B. Potom musíme nejprve vrátit změnu A, provést změnu B a poté znovu spekulativně provést změnu A. Takhle budeme spekulativní změnu A tlačit před sebou, dokud nám server neodpoví, že naše změna byla provedena. V tu chvíli A už není spekulativní a můžeme na ni zapomenout.

Takhle před sebou může každý klient tlačil libovolné množství spekulativních změn, záleží na latenci sítě a aktivitě ostatních klientů. Takové chování by mělo zajistit konzistenci stavů všech dokumentů.


### Dokument

Třída `Document` drží stav dokumentu na serveru. Může na sebe aplikovat změny (třída `Change`), které mu chodí od klientů. Třída není thread-safe a je teoreticky úplně nezávislá na logice serveru.


## Testy

C# testy serverového kódu jsou ve složce `ServerTests` a lze je spouštět pomocí NUnit z Visual Studia.

Klientská aplikace má několik drobných testů přibaleých do kódu aplikace, protože jde hlavně o testy logiky komunikující s editorem *codemirror*. Ty se spustí z konzole webového prohlížeče na stránce místnost příkazem: `mainController.runTests()`. Ale testy byly napsány během vývoje logiky kterou testují a ta se zatím nezměnila. Je možné, že nebudou fungovat, protože nejsou úplně čistě izolované od aplikace a ta teď vypadá trochu jinak.
