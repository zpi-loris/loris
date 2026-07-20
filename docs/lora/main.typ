#import "@local/arkona:0.2.1": arkona, definition
#show: arkona.with(
  title: "Notatki z książki o LoRa",
  authors: (
    (name: "", affiliation: "Politechnika Wrocławska"),
  ),
  abstract: [
    Wszystkie notatki zawarte w tym dokumencie oparte są na książce "LoRa -
    LoRaWAN and Internet of Things for beginners" autorstwa Sylvaina
    Montagny'ego z Universite Savoie Mont Blanc.
  ],
  date: datetime.today().display(),
)
#set cite(style: "chicago-author-date")
#show link: underline

= Systemy wbudowane oraz Internet Rzeczy

== Internet Rzeczy
Termin *IoT* odnosi się do starszego terminu zwanego Machine-to-Machine.\
*Machine-to-Machine* (M2M) to zbiór bezprzewodowych lub przewodowych
technologii sieciowych, pozwalających na *automatyczną wymianę informacji
pomiędzy systemami, bez interwencji człowieka*. Internet Rzeczy to po prostu
szersza wersja M2M, w której urządzenia biorące udział w komunikacji nie są
wyłącznie urządzeniami przemysłowymi, ale też i urządzeniami codziennego
użytku.

=== Systemy wbudowane w Internecie Rzeczy
Mowiąc ogólnie systemy elektroniczne możemy scharakteryzować poprzez ich:
- *zużycie prądu*,
- *moc obliczeniową*,
- *rozmiar*,
- *cenę*.

#figure(image("assets/iot-characteristics.png", height: 34%), caption: [Charakterystyka
  urządzenia IoT])

Pomijając oczywiste wyjątki, zakładamy, że zdecydowana większość wbudowanych
systemów w IoT ma:
- *niskie zużycie prądu*,
- *niewielką moc obliczeniową*,
- *mały rozmiar*,
- *niską cenę*.

=== Protokoły bezprzewodowyej komunikacji w IoT
W świecie IoT używane jest wiele protokołów, które zazwyczaj charakteryzujemy
poprzez ich *przepustowość* (bandwidth) oraz *range* (zasięg).
#line(length: 100%)
#definition("Przepustowość")[
  *Przepustowość łącza* to parametr określający maksymalną ilość danych, jaka może
  zostać przesłana przez dane połączenie sieciowe w określonym czasie. Jest to
  bezpośrednio powiązane z częstotliwością nadawanych fal.
]
#definition("Zasięg")[
  *Zasięg* reprezentuje rzeczywisty dystans operacyjny bezprzewodowej sieci
  komunikacyjnej. Jest to bezpośrednio powiązane z długością fali.
]
#line(length: 100%)
#figure(image("assets/protocols.png"), caption: [Protokoły używane w IoT]) Na
obrazku po prawej dolnej stronie oznaczone są protokoły o niskiej
przepustowości i wysokim zasięgu. Ich największa zaletą jest *bardzo mały pobór
prądu*. Tego typu sieci nazywamy *Low Power Wide Area Network* (LPWANs) i do
nich zaliczamy Lorę.

=== Pasma częstotliwości
Pasmo częstotliwości to po prostu zasięg częstotliwości radiowych (3 kHz - 300
GHz), używanych do transmitowania sygnału poprzez sieć. W Europie, LoRa może
korzystać z pasm częstotliwości: 433 MHz, 868 MHz (LoRaWAN) oraz 2.4 GHz.

=== Fale elektromagnetyczne
#definition("Fale elektromagnetyczne")[
  *Fale elektromagnetyczne* to fale, które *nie potrzebują ośrodka aby przenosić
  energię*, mogą ją przenieść nawet w próżni. Przykładami fal elektromagnetycznych
  są fale radiowe, światło widzialne czy promieniowanie rentgenowskie. Fale
  elektromagnetyczne są powodowane przez zmiany pola magnetycznego i
  elektrycznego.

  Odbywa się no takiej zasadzie, że w fali zarówno pole magnetyczne i pole
  elektryczne oscyluje, oddziaływując tym samym na drugie - nazywamy to
  *indukcją*. Impuls może wyjść zarówno od jednego jak i drugiego pola. W
  sytuacji gdy jedno z dwóch pól zanika, drugie zanika również.

  #figure(
    image("assets/em-waves.png", height: 30%),
    caption: [Fale elektromagnetyczne],
  )

  Długość fali $lambda$ jest odwrotnie proporcjonalne do częstotliwości fali $f$:
  #align(center)[*$lambda = c/f$*]

  W próżni fale elektromagnetyczne rochodzą się ze stałą prędkością równą
  prędkości światła $c approx 300" "000 "km"/s$

  Fale o wyższej długości, a więc o niższej częstotliwości rozchodzą się lepiej
  i działają lepiej na długich odległościach.
]

== Tryby współdzielenia medium
Niezależnie od protokołu, z którego korzystamy, *medium do transferu informacji
jest powietrze* (wszystkie protokoły IoT są bezprzewodowe).

Medium musi być *współdzielone* przez wszystkie transmitery, w taki sposób, żeby
*urządzenia końcowe nie ingerowały pomiędzy sobą*. Właśnie z tego powodu
przydzielamy pasma częstotliwości do każdego przypadku użycia. Na przykład
pasmo częstotliwości radia FM (Frequency Modulation) w Europie wynosi między
87.5 MHz a 108 MHz.

Wewnątrz przydzielonych pasm częstotliwości, urządzenia końcowe mogą
współdzielić medium na wiele różnych sposobów.

=== Multipleksing
#definition("Multipleksing")[
  *Multipleksing* to proces łączenia wielu sygnałów, tak żeby poruszały się
  równocześnie przez jedno łącze. Celem multipleksingu jest *współdzielenie
  medium*.
]

=== FDM (Frequency Division Multiplexing)
Urządzenia korzystają z kanałów częstotliwości, żeby rodzieliać swoje
transmisje. W ten sposób informacje przesyłane są różnymi kanałami, które nie
ingerują ze sobą. LoRa korzysta z tego trybu.

#figure(
  image("assets/FDM.png"),
  caption: [Wykorzystanie FDM dla LoRa],
)

=== TDM (Time Division Multiplexing)
Urządzenia końcowe transmitują dane nieregularnie (z przerwami), aby pozostawić
wolny kanał dla innych. LoRa korzysta z tego trybu, tj., LoRa nie pozwala
urządzeniom na ciągłe przesyłanie. Jednakowoż, ponieważ urządzenia końcowe nie
są zsynchronizowane, mogą wystąpić kolizje.

=== Spread Spectrum (Rozpraszanie widma)
Urządzenia końcowe transmitują *w tym samym momencie, na tym samym kanale*,
lecz z specyficzną strukturą sygnału, która pozwala na odebranie sygnału z
szumu odbiorcy. LoRa korzysta z tego trybu.

#figure(
  image("assets/spectrum.png", height: 30%),
  caption: [Wykorzystanie Spread Spectrum dla LoRa],
)

Urządzenia końcowe korzystające z LoRy mogą wybrać jeden z wielu kanałów do
przesyłu danych. Całe piękno LoRy polega na tym, że nawet jeśli mamy wiele
urządzeń końcowych na tym samym kanale, wszystkie z nich będą w stanie
przesyłać w tym samym momencie.

Najpopularniejsze modulacje z rozpraszaniem widma korzystają z "kodów",
natomiast LoRa używa symbolów zwanych Chirp, stąd też nazwa modulacji *Chirp
Spread Spectrum (CSS)*.

=== Rozpraszanie widma z kodami
Działa to w taki sposób, że odbiornik nasłuchuje wszystkich fal o określonej
częstotliwości. Aby odczytać wiadomość od konkretnego nadawcy, przed
rozpoczęciem komunikaci musi znać jego *kod*. Następnie odbiera cały szum,
który jest wysyłany falami o wybranej częstotliwości i przepuszcza je przez ten
kod. W sytuacji, gdy nadawca wyśle wiadomość, odbiorca jest w stanie przepuścić
tę wiadomość przez swój zapisany kod i odczytać wiadomość nadawcy.

= Propagacja oraz transmisja radiowa

== Jednostki i definicje

=== Decybel (dB)
Kiedy sygnał rozprzestrzenia się po swojej ścieżce komunikacyjnej, stosunek
między *odebraną* i *nadaną mocą* może się znacznie różnić.

W sytuacji gdy używamy kabla stosunek wynosi blisko 1. Gdy używamy wzmacniacza
może być znacznie wyższy, lub może też być bardzo niski w przypadku straty w
trakcie transmisji w powietrzu.

Operowanie tak dużymi i małymi liczbami nie pozwala na szybkie określenie
*wzmocnienia* lub *tłumienia* transmisji. Ponadto mnożenie wzmocnienia
poszczególnych *bloków transmisyjnych* jest niewygodne.

#definition("Blok transmisyjny")[
  *Blok transmisyjny* to każde pojedyncze urządzenie lub podzespół, przez który
  przechodzi sygnał na jego drodze od nadawcy do odbiorcy.
]
#figure(
  image("assets/transmission.png", height: auto),
  caption: [Transmisja mocy pomiędzy nadawcą a odbiorcą],
)
#line(length: 100%)
#definition("Decybel")[
  *dB* to stosunek między dwiema mocami:
  - odbiorcy $P_R$,
  - nadawcy $P_T$.

  Wzór na stosunek w dB jest następujący:
  #align(center)[$"Power ratio (dB)" = 10*log_10(P_R/P_T)$]

  Gdy wynik jest negatywny, mamy do czynienia z *tłumieniem*, gdy pozytywny ze
  *wzmacnianiem*.
]

#pagebreak()

W praktyce najczęściej korzysta się z prostych obliczeń na podstawie tej
tabelki:

#figure(
  image("assets/db.png", height: 15%),
  caption: [Stosunek mocy i decybele],
)

Przykładowo mamy wzmacniacz, który wzmacnia sygnał o 16 dB, zatem jeżeli
dostanie moc o wysokości 1 W, na wyjściu otrzymamy:
#align(center)[$10 "dB" + 3 "dB" + 3 "dB" = 1 W * 10 * 2 * 2 = 20 W$]

#line(length: 100%)

Jak przykładowo dla decybeli dla dźwięku naszym punktem odniesienia są 20 mPa,
to *dla mocy odniesieniem jest 1 mW*. Decybele, które porównujemy do 1 mW,
oznaczamy jako *dBm*.

#figure(
  image("assets/dbm.png", height: 15%),
  caption: [Porównanie mocy w dB i mW],
)
Załóżmy, że mamy walkie-talkie, które ma moc transmisji wynoszącą 2 W:

#align(center)[$1 "mW" * 10 * 10 * 10 * 2 = 10 "dBm" + 10 "dBm" + 10
"dBm" + 3 "dBm" = bold(33 "dBm")$]

#pagebreak()
=== RSSI, czułość, SNR, bilans łącza
Transmiter nadaje sygnał z mocą $P_T$. Odbiornik odbiera tylko część z tej mocy
($P_R$), oraz także trochę szumu ($P_N$). Wartości te podajemy w dB.

#figure(
  image("assets/radio-transmission.png", height: 15%),
  caption: [Transmisja radiowa],
)

- *Received Signal Strength Indication (RSSI)* to moc odebrana $P_R$
- *Czułość* to minimalna moc $P_R$ (minimalna RSSI), która musi być obecna w
  odbiorniku, żeby odebrać sygnał. Jeżeli odebrane RSSI jest poniżej tego
  poziomu, syngał jest niewykrywalny.
- *SNR (Signal over Noise Radio)* to stosunek odebranej mocy $P_R$ do mocy
  szumu $P_N$.
- *Bilans łącza* to różnica pomiędzy mocą transmisji $P_T$, a czułością.

#line(length: 100%)

*Sygnał może być odebrany w przypadku, gdy:*
1. RSSI jest większe niż poziom czułości odbiornika.
2. SNR nie spada poniżej pewnego progu, który skutkowałby brakiem możliwości
  wykrycia sygnału po stronie odbiornika.
#line(length: 100%)

*Co można zrobić gdy RRSI spadnie poniżej poziomu czułości?*
1. *Zwiększenie $P_T$.* Jest to możliwe tylko do pewnego stopnia, ponieważ moc
  transmitowania jest ograniczona przez specyfikację. Maksymalna moc $P_T$ na
  paśmie 868 MHz to 14 dBm (25 mW).

2. *Zwiększenie czułości odbiornika.*
#line(length: 100%)
*Przykład*: \
Transmiter *nadaje 13 dBm* oraz korzysta z anteny ze *wzmocnieniem 2 dB*.
*Strata w powietrzu wynosi 60 dB*. Antena odbiornika ma *wzmocnienie 2 dB* i
jest podłączona do odbiornika z *poziomem czułości -80 dB*.
#align(center)[13 + 2 - 60 + 2 = *-43 dB > -80 dB*]
Bilans łącza w tym przypadku wynosi: *13 + 80 = 93 dB*.

== Odległość transmitowania w LoRa
#definition("Odległość transmitowania LoRa")[
  Nadawana moc $P_T$ jest tłumiona w powietrzu zgodnie z uproszczonym wzorem:
  #align(center)[$"Strata" = 10*log_10("Dystans"^2 * "Częstotliwość"^2 * 1755)$]
  - Strata: *dB*
  - Dystans: *km*
  - Częstotliwość: *MHz*

  Możemy zatem przekształcić wzór, aby obliczyć maksymalny dystans:
  #align(center)[$"Dystans" = sqrt(10*"Strata"/10 / (1755*"Częstotliwość"^2))$]

  Z kolei wiemy, że bilans łącza mówi nam o maksymalnej stracie, która może
  dotknąć transmisję:

  #align(center)[$"Dystans" = sqrt(10*"Bilans łącza"/10 / (1755*"Częstotliwość"^2))$]

  - Nadajnik LoRa SX1272 (bilans 157 dB) daje teoretyczną odległość 1946 km.
  - Nadajnik LoRa SX1262 (bilans 170 dB) daje teoretyczną odległość 8696 km.
]
#pagebreak()
= Warstwa fizyczna w LoRa

== Modulacja w LoRa
Modulacja w LoRa korzysta z *rozpraszania widma* do transmitowania danych. Nie
używa ona jednak wcześniej wspomnianego rozpraszania widma z użyciem kodu, lecz
korzysta z *Chirpów*, dlatego też modulację w LoRa nazywamy
*Chirp Spread Spectrum*.

Cel pozostaje ten sam -- możliwość wielu transmisji w tym samym momencie i na
tym samym kanale.

#definition("Modulacja")[
  *Modulacja* to proces kodowania informacji w fale elektromagnetyczne. Odbywa
  się to poprzez nałożenie *sygnału modulacyjnego* na *falę nośną* z
  wykorzystaniem jednej z technik modulacji (np. AM lub FM). Następnie tak
  zmodulowana fala przenosi w sobie *informacje*, które po stronie odbiornika
  możemy zdemodulować.

  #figure(
    image("assets/am-modulation.png", height: 43%),
    caption: [Modulacja AM (Amplitude Modulation)],
  )
]

=== Chirp
Sygnał emitowany przez modulację LoRa to *symbol* o podstawowej formie widocznej
na poniższym obrazku. Nazwa "Chirp" wynika z faktu, że ten symbol jest używany
w technologii radarów (Chirp: Compressed High Intensity Radar Pulse).

#figure(
  image("assets/chirp.png", height: 40%),
  caption: [Chirp],
)

Częstotliwość kanału $F_"kanał"$ to centrum częstotliwości, a pasmo zajmowane
wokół niego to *pasmo przenoszenia* (bandwidth).

#figure(
  image("assets/lora-transmission.png", height: auto),
  caption: [Spektrum częstotliwości dla transmisji LoRa],
)

#figure(
  image("assets/spectogram.png", height: auto),
  caption: [Spektogram transmisji LoRa],
)

#definition("Symbol")[
  *Symbol* to stan fali nośnej. Określa się go poprzez konkretną fazę,
  amplitudę oraz częstotliwość. Ponadto tempo, z jakim fala nośna zmienia swój
  stan z jednego symbolu na kolejny, nazywane jest *szybkością modulacji* (lub
  szybkością symbolową).

  *W LoRa symbolem jest 1 chirp.*
]
#line(length: 100%)
W LoRa każdy *symbol* reprezentuje numer nadanych bitów. Ogólna zasada jest taka:
#align(center)[*Liczba nadanych bitów w jednym symbolu = Spreading Factor*]

#definition("Spreading Factor")[
  *Spreading Factor* w LoRa reprezentuje liczbę nadanych bitów w jednym symbolu.

  W trakcie emisji, bity są grupowane w pakiety o liczbe *SF* bitów. Każdy
  pakiet jest reprezentowany przez konkretny symbol spośród $2^"SF"$ możliwych.
  Jedyną różnicą dla symbolów jest fakt, że *każdy symbol rozpoczyna się od innej
  częstotliwości, która reprezentuje pakiet bitów.*
]

#figure(
  image("assets/SF2.png", height: auto),
  caption: [Nadane symbole dla modulacji LoRa SF2],
)
#line(length: 100%)
*Przykład*
- Mamy kod binarny: `0 1 0 1 1 1 0 0 0 1 1 0 0 1 0 0 1 1 0 1`
- Korzystamy z SF10

Grupujemy bity w pakiety po 10. Każdy pakiet jest reprezentowany przez
konkretny symbol. Istnieją 1024 symbole do zenkodowania 1024 możliwych
kombinacji binarnych ($2^10$).

#figure(
  image("assets/SF10.png", height: auto),
  caption: [Nadane symbole dla modulacji LoRa SF10],
)

#pagebreak()
=== Czas nadawania symbolów
Czas nadawania symbolów ($T_"symbol"$) zależy od *Spreading Factoru*. Im wyższy
SF, tym dłuższy czas nadawania. Dla tego samego pasma przenoszenia, czas
nadawania symbolu w SF8 będzie dwukrotnie dłuższy niż SF7, SF9 dwukrotnie
wyższy od SF8 itd. aż do SF12 (najwyższy SF).

#figure(
  image("assets/transmission-time.png", height: auto),
  caption: [Czas nadawania symbolów],
)

Czas nadawania $T_"symbol"$ zależy też od *pasma przenoszenia (bandwidth)*, z
którego korzystamy. $T_"symbol"$ jest odwrotnie proporcjonalny do pasma
przenoszenia:

#align(center)[$T_"symbol" = 2^"SF"/"Bandwidth"$]

#figure(
  image("assets/time-example.png", height: 18%),
  caption: [Czas nadawania symbolów dla różnych SF. Bandwidth 125 KHz],
)
#pagebreak()
=== Time on Air
Time on Air to całkowity czas trwania jednej ramki LoRa. Zależy ona od liczby
symbolów w ramce: payloadu, preambuły, nagłówka oraz sumy kontrolnej CPC.

#figure(
  image("assets/time-on-air.png", height: auto),
  caption: [Time on air ramki LoRa],
)
#align(center)[* $"Time on Air" = n_"symbol" * T_"symbol"$ *]
#align(center)[gdzie $n_"symbol"$ to liczba symbolów w ramce LoRa.]


== Bit rate

#definition("Bitrate")[
  *Bitrate* to ilość bitów transmitowanych w jednostce czasu poprzez kanał
  komunikacyjny.

  Najczęściej mierzona w bitach na sekundę i ich wielokrotnościach.
]
#line(length: 100%)
Wzór na Bitrate dla LoRa:
#align(center)[* $"Bitrate" = "SF"*"Bandwidth"/2^"SF"$ *]

- Im większy bandwidth tym *wyższy bitrate*
- Im wyższy SF tym *mniejszy bitrate*

=== Wpływ Code Rate na bitrate

#definition("Code Rate")[
  *Code Rate* -- proporcja strumieniowanych danych, która jest użyteczna, tj.
  *proporcja $k/n$*, gdzie:
  - k - informacje użyteczne,
  - n - całkowita liczba informacji,
  - $n - k$ - informacje redundantne.

  W przypadku $"CR" = 4/8$, 8 bitów jest transmitowanych za każdym razem, gdzie
  w rzeczywistości chcemy przesłać jedynie 4 bity.

  W tym przypadku mamy *Overhead Ratio* (wskaźnik kosztów) wynoszący 2, co
  oznacza, że mamy dwukrotną liczbę transmitowanych bitów.

  *Po co Code Rate?*\
  Nadmiarowe informacje umieszczamy po to, żeby w przypadku utraty części
  informacji w trakcie transmisji, mieć dodatkowy zasób, do którego możemy
  sięgnąć i naprawić błąd. Formalnie nazywamy to *Forward Error Correction
  (FEC)*.
]

#figure(
  image("assets/code-rate.png", height: auto),
  caption: [Overhead Ratio i Code Rate],
)

*Przykład*:
- Mamy dwa przypadki transmisji jeden: SF7, 125 kHz i drugi: SF12, 125 kHz z CR
  na poziomie 4/5. Jaki będzie rzeczywisty bitrate?

  1. SF7, 125 kHz $=>$ $(6,836 "b/s") / 1.25 = 5469 "b/s"$

  2. SF12, 125 kHz $=>$ $(366 "b/s") / 1.25 = 293 "b/s"$

#pagebreak()

=== Wpływ Overheadu na bitrate
#definition("Ramka LoRa")[
  #figure(
    image("assets/frame.png", height: auto),
    caption: [Ramka LoRa],
  )

  Ramka LoRa składa się z:
  - *preambuły* pozwalającej na synchronizację nadawcy z odbiorcą,
  - opcjonalnych *nagłówków* po preambuli,
  - *PHY payload*, czyli właściwych przesyłanych danych,
  - pola *CRC* sprawdzającego integralność danych.
]

Z racji, że wysyłamy dodatkowe pakiety w jednej ramce LoRa, dla każdego ładunku
(payload), musimy *przesłać dodatkowe bity dla całej ramki*. To znacznie
spowalnia nasz bitrate, ponieważ nie przesyłamy już tylko samego ładunku.

Gdy korzsytamy z protokołu *LoRaWAN*, do ramki dołączane są jeszcze nagłówki z
LoRaWAN'u co dodatkowo spowalnia bitrate.

Co więcej na paśmie 868 MHz, wedle regulacji UE urządzenia końcowe *nie mogą
transmitować więcej niż 1% czasu*. Oznacza to, że dla dowolnej jednostki czasu,
gdy urządzenie transmituje przez 1 musi pozostawać w spoczynku przez 99.

Ostatecznie sumując te wszystkie "spowolnienia" bitrate'u pozostaje nam:
- SF7, 125 kHz, CR4/5 $=>$ 1.73 b/s
- SF12, 125 kHz, CR4/5 $=>$ 0.07 b/s
Więc jak widać nie za wiele, dlatego też LoRa nie może być wykorzystywana do
przesyłania danych o dużych rozmiarach, bo zajmie to wieki.

#pagebreak()

== Symulacja transmisji LoRa

=== Time on Air
Najpierw wyliczamy czas trwania jednej ramki, czyli _Time on Air_:

#align(center)[* $"Time on Air" = n_"symbol"*T_"symbol"$ *]

- $n_"symbol"$ to liczba symbolów w ramce LoRa.
- $T_"symbol"$ to czas trwania jednego symbolu.

#figure(
  image("assets/n-symbol.png", height: auto),
  caption: [Wzór na obliczenie $n_"symbol"$],
)

=== Modulacja Chirp
Modulacja LoRa generuje Chirpy -- *sygnał modulujący symbol(t)* wymieszane z
lokalnym sygnałem oscylatora -- *falą nośną channel(t)* w celu przemieszczenia
Chirpa po częstotliwości kanału $f_"channel"$ i w konsekwencji wygenerowania
gotowego do wysłania w eter sygnału lora(t)

#figure(
  image("assets/modulation-example.png", height: 15%),
  caption: [Proces modulacji],
)

=== Demodulacja Chirp
Proces demodulacji składa się z 2 etapów:

1. Zamiana lora(t) na pasmo podstawowe, w celu znalezienia sygnału symbol(t).
2. Jest on następnie mieszany z sygnałem down(t), który jest sygnałem typu
  *down-chirp* (chirpem malejącym). Wynikowy sygnał demod(t) jest filtrowany, a
  następnie używamy algorytmu FFT (Szybkiej Transformaty Fouriera), aby
  odzyskać *indeks symbolu n*, a tym samym 12 bitów.

#figure(
  image("assets/demodulation.png", height: 15%),
  caption: [Proces demodulacji],
)

#figure(
  image("assets/demodulation-2.png", height: 55%),
  caption: [Wyniki mieszania lora(t) z down(t)],
)

#figure(
  image("assets/demodulation-3.png", height: auto),
  caption: [Przykład demodulacji symbolu z wieloma wartościami],
)

=== Rzeczywisty test transmisji LoRa
Test przeprowadzony dla parametrów:
- Kanał: 868.1 Mhz
- Spreading Factor: 12
- Bandwidth: 125 kHz
- Coding Rate: 1.25 (4/5)
- Number of preamble: 8
- LoRa PHY payload: "HELLO"

#figure(
  image("assets/spectogram-real.png", height: auto),
  caption: [Spektogram ramki LoRa],
)
- 8 pierwszych Chirpów to preambuła,
- kolejne 4.25 (2 w góre i 2.25 w dół) to symbole synchronizacyjne oznaczające
  start ramki.

#figure(
  image("assets/spectrum-real.png", height: auto),
  caption: [Spektrum częstotliwości dla transmisji LoRa],
)

=== Zużycie energii
Stqandard LoRaWAN ma na celu aplikację o bardzo niskim poziomie zużycia
energii. Często bateria starcza nawet na kilka lat. Zużycie energii zależy
m.in. od:
- ilość danych do transportu (payload),
- Spreading Factoru,
- możliwych kolizji przy emisji (tym samym retransmisji),
- żądania potwierdzenia odbioru nadanych ramek,
- cyklu pracy,
- mocy nadawczej _transcievera_,
- mocy pobieranej w trybie _standby_ między transmisjami.

#pagebreak()

= LoRaWAN

*Różnica między LoRa a LoRaWAN:*\

LoRa to *typ modulacji* używany pomiędzy dwoma urządzeniami końcowymi LoRa lub
pomiędzy urządzeniem końcowym a bramką. Kiedy mówimy o całym *łańcuchu
komunikacji* (od urządzenia końcowego do serwera LoRaWAN), mówimy o standardzie
*LoRaWAN*. LoRaWAN to rozszerzenie protokołu LoRa, dające *możliwości bezpiecznego
połączenia* urządzenia do serwera w celu przekazania danych do użytkownika.

- *Warstwa fizyczna LoRa*: Typ modulacji (CSS) i fizyczny format ramki, używany
  do przesyłania danych między nadawcą a odbiornikiem.
- *Standard LoRaWAN*: Architektura sieci (urządzenia końcowe, bramki, serwery)
  oraz bardziej specyficzny format ramki, pozwalający na bezpieczne
  transmitowanie danych do serwera LoRaWAN.

== Struktura sieci LoRaWAN

#figure(
  image("assets/LoRaWAN.png", height: auto),
  caption: [Widok z lotu ptaka na całkowitą architekturę sieci LoRaWAN],
)

Urządzenia końcowe, bramki, serwer oraz serwer aplikacji są sercem architektury
LoRaWAN, jednakże platforma IoT oraz połączenie z użytkownikiem nie ma nic
wspólnego z LoRaWAN -- jest to klasyczny serwis webowy.

#pagebreak()

=== Urządzenia końcowe
*Urządzenie końcowe LoRaWAN* to elektroniczny system wbudowany, należący do
świata IoT, cechuje go: niskie zużycie mocy, mały rozmiar i niewielki koszt.
Urządzenie takie ma własny *transciever*, aby skomunikować się z bramką.

Urządzenie końcowe LoRaWAN nie musi komunikować się tylko z jedną bramką,
wszystkie bramki dostępne w obszarze działania dostają wiadomości od
urządzenia i je przetwarzają.
#line(length: 100%)
=== Bramka LoRaWAN
Bramki LoRaWAN nasłuchują wszystkich kanałów (w paśmie LoRaWAN) i Spreading
Factors'ów naraz. Kiedy ramka LoRa zostaje odebrana, bramka transmituje jej
zawartość poprzez Internet do serwera, który został uprzednio zkonfigurowany w
celu obsługi bramki.

#figure(
  image("assets/gateway.png", height: auto),
  caption: [Bramka LoRaWAN],
)

Każda bramka LoRaWAN ma unikalny indentyfikator (64-bitowy EUI). ID to, jest
użyteczne w celu rejestracji i aktywacji bramki na serwerze sieci.

#pagebreak()

=== Serwer sieci
*Serwer sieci* otrzymuje wiadomości transmitowane przez bramki i usuwa
zduplikowane pakiety (jak wspominałem wcześniej wiele bramek może obsługiwać te
same urządzenia końcowe). Wtedy Serwer sieci *uwierzytelnia* (nie szyfruje!!)
tą wiadomość, dzięki 128-bitowemu kluczowi AES, zwanemu *NwkSKey* (Network
Session Key).

#figure(
  image("assets/network-server.png", height: 25%),
  caption: [Uwierzytelnianie pomiędzy urządzeniem końcowym a serwerem sieci],
)

- Jeżeli uwierzytelnienie się *nie powiedzie*, wiadomość jest odrzucana.
- Jeżeli uwierzytelnienie się *powiedzie*, serwer sieci przesyła wiadomość
  dalej do *serwera aplikacji*
#line(length: 100%)
=== Serwer aplikacji
Serwer aplikacji odbiera zaszyfrowane wiadomości z serwera sieci. Szyfrowanie i
odszyfrowywanie jest wykonywane dzięki 128-bitowemu kluczowi AES, zwanemu
*AppSKey* (Application Session Key).

#figure(
  image("assets/app-server.png", height: 25%),
  caption: [Szyfrowanie pomiędzy urządzeniem końcowym a serwerem aplikacji],
)

=== Platforma IoT
Platforma IoT to interfejs aplikacji użytkownika, składa się ona z:

1. Połączenia z serwerem aplikacji w celu zbierania danych. Zazwyczaj
  wykorzystuje się do tego protokoły *HTTP* lub *MQTT*. W fazie deweloperskiej
  czasami wykorzystuje się do tego niezabezpieczone wersje tych protokołów. Po
  zdeployowaniu aplikacji naturalnie używamy już wersji zabezpieczonych, czyli
  *HTTPS* i *MQTTS*.
2. Bazy danych do przechowywania informacji.
3. Dashboardu dostępnego dla użytkownika przez stronę internetową lub aplikację
  mobilną.

#figure(
  image("assets/iot-platform.png", height: auto),
  caption: [Połączenie pomiędzy serwerem aplikacji a platformą IoT],
)
