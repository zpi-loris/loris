#import "@local/arkona:0.2.1": arkona, attention, definition
#show: arkona.with(
  title: "Chirpstack",
  authors: (
    (name: "", affiliation: "Politechnika Wrocławska"),
  ),
  abstract: [
    Opis architektury i działania systemu Chirpstack \ na potrzeby projektu loris.
  ],
  date: datetime.today().display(),
)
#set cite(style: "chicago-author-date")
#show link: underline

= Bramka
== Stawianie bramki na RPi 5

Istnieją dwie główne opcje:
1. wykorzystanie gotowego systemu operacyjnego `Chirpstack Gateway OS`,
2. wykorzystanie oprogramowania `Chirpstack Concentratord` i odpowiedniego
  przekaźnika otrzymywanych pakietów, np. `Chirpstack MQTT Forwarder`.

Wykorzystamy drugi sposób, aby zachować elastyczność systemu, np. możliwość
jednoczesnego postawienia na bramce serwera Chirpstack.

#align(center)[
  #figure(caption: [Architektura bramki])[
    #image("assets/architektura_bramki.svg")
  ]
]

=== Chirpstack Concentratord
`Chirpstack Concentratord` to demon koncentratora, który pod spodem wykorzystuje
oficjalne HALe od Semtech. Koncentrator udostępnia API poprzez ZeroMQ, dzięki
czemu możemy wykorzystać wiele niezależnych przekaźników.

Przydatne linki:
- #link("https://artifacts.chirpstack.io/downloads/chirpstack-concentratord/")[pobieranie]
  -- można pobrać nawet paczkę `.deb`; nas interesuje `chirpstack-concentratord-sx1302`,
- #link("https://www.chirpstack.io/docs/chirpstack-concentratord/configuration.html")[konfiguracja koncentratora]
  #footnote[*WAŻNE* -- na modułach SX1302 nie ustawiamy sami ID bramki,
    tylko jest ono zaszyte w samym module, z którego odczytuje je Chirpstack].

=== Chirpstack MQTT Forwarder
`Chirpstack MQTT Forwarder` odbiera pakiety od koncentratora i przekazuje po
MQTT do serwera Chirpstack.

Przydatne linki:
- #link("https://artifacts.chirpstack.io/downloads/chirpstack-mqtt-forwarder/")[pobieranie],
- #link("https://www.chirpstack.io/docs/chirpstack-mqtt-forwarder/configuration.html")[konfiguracja].

