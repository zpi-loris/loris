# Opis projektu
Głównym celem projektu jest wytworzenie rozproszonego systemu do zdalnego
monitorowania roślin. System składałby się z wielu urządzeń monitorujących
i jednego punktu centralnego służącego do analizowania zebranych danych,
podejmowania decyzji na ich podstawie oraz zarządzania całym systemem.

## Potencjalne zastosowania
1. monitorowanie pól rolniczych poprzez odpowiednie rozstawienie wielu czujników
   na polach;
2. monitorowanie roślin w domach, szklarniach itp.;
3. monitorowanie ogrodów;
4. ...

# Kluczowe funkcjonalności
1. monitorowanie poniższych parametrów:
    1. nasłonecznienie,
    2. wilgotność gleby,
    3. ph gleby,
    4. stężenie azotu, fosforu, potasu (tzw. NPK) w glebie.
2. udostępnienie API sieciowego, które pozwoliłoby na integrację systemu z
   istniejącymi już systemami, np. automatycznego nawadniania;
3. udostępnienie interfejsu webowego agregującego dane ze wszystkich stacji,
   umożliwiającego analizę zebranych danych oraz nadzorowanie pracy systemu
4. Spełnienie normy szczelności IP65 przez stacje mierzące,

# Dodatkowe funkcjonalności
Poniższe funkcjonalniści mają niższy priorytet i zostaną zrealizowane, jeśli
wystarczy na to czasu oraz zasobów, a kluczowe funkcjonalności zostaną już
zrealizowane:
1. własny system automatycznego nawadniania roślin,
2. aktualizacje OTA urządzeń monitorujących,
3. aplikacja mobilna odpowiadająca funkcjonalnością interfejsowi webowemu,
4. system `Find me` - dźwiękowy i/lub wizualny system ułatwiający zlokalizowanie
   konkretnego urządzenia monitorującego,
5. zasilanie stacji mierzących za pomocą paneli fotowoltaicznych,
6. Spełnienie normy szczelności IP67 przez stacje mierzące,

# Opis technologiczny
Poniższy opis jest jedynie wstępnym założeniem i może ulec znaczącym zmianom.


## Stacje mierzące
Każda stacja mierząca to jeden mikrokontroler (np. ESP32), do którego podłączone
są odpowiednie czujniki oraz moduł do komunikacji technologią LoRa.

## Komunikacja
Komunikacja podzielona jest na dwa etapy:
1. Komunikacja od stacji mierzących do bramki LoRaWAN za pomocą protokołu
   LoRaWAN
2. Komunikacja od bramki LoRaWAN do serwera (punktu centralnego) za pomocą WiFi
   (lub innej podobnej technologii), wykorzystując protokół MQTT,

Taki podział komunikacji wymaga skonstruowania dodatkowego urządzenia - w.w.
bramki LoRaWAN, aby umożliwić bezpieczną i wydajną komunikację w formie wiele do
1, a następnie przesłanie danych do głównego serwera.

Komunikacja LoRaWAN (urządzenia <---> bramka) będzie wykorzystywała szyfrowanie
AES 128-bitowe.
Komunikacja protokołem MQTT będzie wykorzystywała protokół TLS.

## Serwer (punkt centralny)
Dowolne urządzenie z ogólnodostępną lub dedykowaną (np. wytworzoną za pomocą
Yocto) dystrybucją Linuxa.

Oprogramowanie serwera będzie konteneryzowane, co znacząco zwiększy
"przenaszalność" systemu.

## Zasilanie
Bramka LoRaWAN oraz serwer będą stale podłączone do sieci elektrycznej.

Stacje mierzące będą zasilane za pomocą akumulatorów oraz (potencjalnie) paneli
słonecznych.
# Opis projektu
Głównym celem projektu jest wytworzenie rozproszonego systemu do zdalnego
monitorowania roślin. System składałby się z wielu urządzeń monitorujących
i jednego punktu centralnego służącego do analizowania zebranych danych,
podejmowania decyzji na ich podstawie oraz zarządzania całym systemem.

## Potencjalne zastosowania
1. monitorowanie pól rolniczych poprzez odpowiednie rozstawienie wielu czujników
   na polach;
2. monitorowanie roślin w domach, szklarniach itp.;
3. monitorowanie ogrodów;
4. ...

# Kluczowe funkcjonalności
1. monitorowanie poniższych parametrów:
    1. nasłonecznienie,
    2. wilgotność gleby,
    3. ph gleby,
    4. stężenie azotu, fosforu, potasu (tzw. NPK) w glebie.
2. udostępnienie API sieciowego, które pozwoliłoby na integrację systemu z
   istniejącymi już systemami, np. automatycznego nawadniania;
3. udostępnienie interfejsu webowego agregującego dane ze wszystkich stacji,
   umożliwiającego analizę zebranych danych oraz nadzorowanie pracy systemu
4. Spełnienie normy szczelności IP65 przez stacje mierzące,

# Dodatkowe funkcjonalności
Poniższe funkcjonalniści mają niższy priorytet i zostaną zrealizowane, jeśli
wystarczy na to czasu oraz zasobów, a kluczowe funkcjonalności zostaną już
zrealizowane:
1. własny system automatycznego nawadniania roślin,
2. aktualizacje OTA urządzeń monitorujących,
3. aplikacja mobilna odpowiadająca funkcjonalnością interfejsowi webowemu,
4. system `Find me` - dźwiękowy i/lub wizualny system ułatwiający zlokalizowanie
   konkretnego urządzenia monitorującego,
5. zasilanie stacji mierzących za pomocą paneli fotowoltaicznych,
6. Spełnienie normy szczelności IP67 przez stacje mierzące,

# Opis technologiczny
Poniższy opis jest jedynie wstępnym założeniem i może ulec znaczącym zmianom.


## Stacje mierzące
Każda stacja mierząca to jeden mikrokontroler (np. ESP32), do którego podłączone
są odpowiednie czujniki oraz moduł do komunikacji technologią LoRa.

## Komunikacja
Komunikacja podzielona jest na dwa etapy:
1. Komunikacja od stacji mierzących do bramki LoRaWAN za pomocą protokołu
   LoRaWAN
2. Komunikacja od bramki LoRaWAN do serwera (punktu centralnego) za pomocą WiFi
   (lub innej podobnej technologii), wykorzystując protokół MQTT,

Taki podział komunikacji wymaga skonstruowania dodatkowego urządzenia - w.w.
bramki LoRaWAN, aby umożliwić bezpieczną i wydajną komunikację w formie wiele do
1, a następnie przesłanie danych do głównego serwera.

Komunikacja LoRaWAN (urządzenia <---> bramka) będzie wykorzystywała szyfrowanie
AES 128-bitowe.
Komunikacja protokołem MQTT będzie wykorzystywała protokół TLS.

## Serwer (punkt centralny)
Dowolne urządzenie z ogólnodostępną lub dedykowaną (np. wytworzoną za pomocą
Yocto) dystrybucją Linuxa.

Oprogramowanie serwera będzie konteneryzowane, co znacząco zwiększy
"przenaszalność" systemu.

## Zasilanie
Bramka LoRaWAN oraz serwer będą stale podłączone do sieci elektrycznej.

Stacje mierzące będą zasilane za pomocą akumulatorów oraz (potencjalnie) paneli
słonecznych.