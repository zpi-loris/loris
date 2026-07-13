#import "@local/arkona:0.1.1": arkona, definition
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
natomiast LoRa używa symboli zwanych Chirp, stąd też nazwa modulacji *Chirp
Spread Spectrum (CSS)*.

=== Rozpraszanie widma z kodami
Działa to w taki sposób, że odbiornik nasłuchuje wszystkich fal o określonej
częstotliwości. Aby odczytać wiadomość od konkretnego nadawcy, przed
rozpoczęciem komunikaci musi znać jego *kod*. Następnie odbiera cały szum,
który jest wysyłany falami o wybranej częstotliwości i przepuszcza je przez ten
kod. W sytuacji, gdy nadawca wyśle wiadomość, odbiorca jest w stanie przepuścić
tę wiadomość przez swój zapisany kod i odczytać wiadomość nadawcy.



=== Modulacja
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



