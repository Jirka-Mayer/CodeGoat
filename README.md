Code goat
=========

> Zápočtový program na .NET I, letní semestr 2019, Jiří Mayer

> Webový textový editor umožňující současnou editaci více klienty

Specifikace zadání: [specification/2019-06-12.md](specification/2019-06-12.md)


## Spouštění

Pro spustění serveru je třeba otevřít projekt (`.sln`) ve Visual Studiu a spustit. Neboo pokud je server zkompilovaný, tak lze manuálně spustit soubor `Server/bin/Debug/Server.exe`.

Pokud dojde ke změně kódu klienta, je třeba ho překompilovat:

    cd Client     # je třeba být ve složce 'Client'
    npm run dev   # nebo 'dev' nebo 'watch', viz. npm balíček 'laravel-mix'

K serveru se lze připojit z prohlížeče na adresu `localhost:8080`.
