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

Termin *IoT* odnosi się do starszego terminu zwanego Machine-to-Machine.\
*Machine-to-Machine* (M2M) to zbiór bezprzewodowych lub przewodowych
technologii sieciowych, pozwalających na *automatyczną wymianę informacji
pomiędzy systemami, bez interwencji człowieka*. Internet Rzeczy to po prostu
szersza wersja M2M, w której urządzenia biorące udział w komunikacji nie są
wyłącznie urządzeniami przemysłowymi, ale też i urządzeniami codziennego
użytku.

== Systemy wbudowane w Internecie Rzeczy

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

== Protokoły bezprzewodowyej komunikacji w IoT
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

== Pasma częstotliwości

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
