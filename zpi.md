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
2. udostępnienie API sieciowego, które pozwoliłoby na integrację systemu z
   istniejącymi już systemami, np. automatycznego nawadniania;
3. udostępnienie interfejsu webowego agregującego dane ze wszystkich stacji,
   umożliwiającego analizę zebranych danych oraz nadzorowanie pracy systemu,
4. spełnienie normy szczelności IP65 przez stacje mierzące.

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
Poniższy opis jest jedynie wstępnym założeniem i może ulec zmianom w zależności
od dostępności części i innych podobnych czynników.

## Stacje mierzące
Każda stacja mierząca to jeden mikrokontroler (np. ESP32), do którego podłączone
są odpowiednie czujniki oraz moduł do komunikacji technologią LoRa.

Jedna stacja mierząca składa się z:
- ESP32

**Czujniki:**
- Czujnik natężenia światła cyfrowy BH1750 lub analogowy ALS-PT19
- Czujnik wilgotności gleby DFRobot Gravity SEN0193
- Czujnik temperatury analogowy DS18B20
LUB
- czujnik wilgotności, temperatury oraz ciśnienia cyfrowy BME280 (3.3V)

**Zasilanie:**
- Bateria litowo-jonowa 18650 - 3.7V

**Komunikacja**:
- Moduł radiowy RA-02 do LoRa

Opcjonalnie:
- moduł ładujący baterię z wykorzystaniem ogniwa słonecznego Waveshare 16120

## Komunikacja
Komunikacja podzielona jest na dwa etapy:
1. Komunikacja od stacji mierzących do bramki LoRaWAN za pomocą protokołu
   LoRaWAN;
2. Komunikacja od bramki LoRaWAN do serwera (punktu centralnego) za pomocą WiFi
   (lub innej podobnej technologii), wykorzystując protokół MQTT.

Taki podział komunikacji wymaga skonstruowania dodatkowego urządzenia - ww.
bramki LoRaWAN, aby umożliwić bezpieczną i wydajną komunikację w formie wiele do
1, a następnie przesłanie danych do głównego serwera.

Komunikacja LoRaWAN (urządzenia <---> bramka) będzie wykorzystywała szyfrowanie
AES 128-bitowe.
Komunikacja protokołem MQTT będzie wykorzystywała protokół TLS.

Dodatkowo, do komunikacji w kilku miejscach, w celu zwiększenia wydajności
przesyłania danych, może zostać wykorzystany format wymiany danych *Protocol
Buffers* (tzw. Protobuf) opracowany przez Google.

## Bramka LoRaWAN

Bramka LoRaWAN składa się z następujących części:
- Raspberry Pi 5;
- Modułu *Wio-WM1302 Long Range Gateway Module without SX1262(SPI) - EU868*
  firmy Seeed Studio. Moduł ten bazowany jest na czipie SX1302 firmy Semtech i
  pracuje na częstotliwości 862 MHz, która jest nielicencjonowana w Europie;
- Płytki HAT (*Hardware Attached on Top*) dedykowanej do ww. modułu firmy Seeed
  Studio;
- Anteny 862 MHz ze złączem SMA;
- Anteny GPS ze złączem IPEX (dostępnej w zestawie z płytką HAT). 

Czip Semtech SX1302 umożliwia komunikację jednocześnie z aż 16 urządzeniami,
zatem przy odpowiedniej konfiguracji do bramki mogłyby być podłączone setki
urządzeń. Do jego obsługi wykorzystana zostanie oficjalnia biblioteka
dostarczająca HAL (*Hardware Abstraction Layer*) od producenta czipu. 

### Linki
- HAL dla SX1302: https://github.com/Lora-net/sx1302_hal
- Płytka HAT: https://www.seeedstudio.com/WM1302-Pi-Hat-p-4897.html
- Moduł LoRa: https://www.seeedstudio.com/WM1302-LoRaWAN-Gateway-Module-Without-SX126-SPI-EU868-p-5847.html
- Zestaw potrzebnych anten: https://www.seeedstudio.com/LoRa-Antenna-Kit-for-reTerminal-DM-p-5714.html

## Serwer (punkt centralny)
Dowolne urządzenie z ogólnodostępną lub dedykowaną (np. wytworzoną za pomocą
Yocto) dystrybucją Linuxa.

Oprogramowanie serwera będzie konteneryzowane, co znacząco zwiększy
"przenaszalność" systemu.

## Zasilanie
Bramka LoRaWAN oraz serwer będą stale podłączone do sieci elektrycznej.

Stacje mierzące będą zasilane za pomocą akumulatorów oraz (potencjalnie) paneli
słonecznych.
