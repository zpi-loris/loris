# Bramka LoRaWAN

> To będzie można przenieść do Typsta potem

## Wykorzystane elementy

1. RaspberryPi 5 z systemem RaspberryPiOS
2. [Seeed Studio WM1302 Raspberry Pi Hat](https://www.seeedstudio.com/WM1302-Pi-Hat-p-4897.html)
3. [Seeed Studio WM1302 LoRaWAN Gateway Module](https://www.seeedstudio.com/WM1302-LoRaWAN-Gateway-Module-Without-SX126-SPI-EU868-p-5847.html)
4. [Seeed Studio LoRa Antenna Kit](https://www.seeedstudio.com/LoRa-Antenna-Kit-for-reTerminal-DM-p-5714.html)
5. Antena GPS

## Budowa fizyczna

Do RaspberryPi (1) należy zamontować Hat WM1302 (2), a następnie w złącze ANT
na Hat'cie wpiąć antenę GPS (5). Po podpięciu antenty GPS  należy zamontować
moduł WM1302 (3) w złącze mini-PCIe na Hat'cie i do złącza RFIO na module wpiąć
antenę z zestawu (4).

> Przed pierwszym uruchomieniem **zawsze** trzeba podłączyć **obie** anteny,
> inaczej ich układy mogą ulec uszkodzeniu.

## Obsługa oprogramowania

Na RaspberryPi należy włączyć magistralę SPI0 za pomocą narzędzia `raspi-config`
lub dodając linię `dtparam=spi=on` do pliku `/boot/firmware/config.txt` (zgodnie
z [oficjalną dokumentacją](https://www.raspberrypi.com/documentation/computers/config_txt.html)).

Następnie należy upewnić się, czy magistrala SPI0 jest w pełni zarządzana i
wykrywana przez system operacyjny za pomocą komendy `ls /dev/spidev0.*` i czy
przypisane do niej zostały odpowiednie piny komendą `pinctrl get 8-11`.

Oczekiwany wynik polecenia `pinctrl`:
```shell
 8: op dh pu | hi // GPIO8 = output
 9: a0    pn | lo // GPIO9 = SPI0_MISO
10: a0    pn | lo // GPIO10 = SPI0_MOSI
11: a0    pn | lo // GPIO11 = SPI0_SCLK
```
