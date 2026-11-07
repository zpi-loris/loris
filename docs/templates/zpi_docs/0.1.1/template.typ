#let boxcounting = "heading"
#let boxnumbering = "1.1.1.1.1.1"

#let zpi_docs(
  title: "",
  authors: (),
  mentor: "",
  body,
  year: "",
  keywords: (),
) = {
  set document(author: authors, title: title)
  set page(
    margin: (left: 20mm, right: 20mm, top: 25mm, bottom: 30mm),
    numbering: "1.",
    number-align: center,
  )
  set text(font: "TeX Gyre Termes", lang: "pl", size: 12pt)
  show math.equation: set text(weight: 400)
  show math.equation: set block(spacing: 0.65em)
  set math.equation(numbering: "(1)")
  set heading(numbering: "1.1.")

  // Nice background for code samples
  show raw.where(block: true): it => {
    let i = 0
    set par(justify: false)
    grid(
      columns: (100%, 100%),
      column-gutter: -100%,
      block(width: 100%, inset: 1em, for line in it.text.split("\n") {
        i += 1
        box(width: 0pt, align(right, str(i) + h(2em)))
        linebreak()
      }),
      block(fill: luma(248), stroke: 1pt + black, width: 100%, inset: 1em, it),
    )
  }

  align(center)[
    #block(text(weight: 1000, 1.75em, "Politechnika Wrocławska"))
    #block(text(weight: 1000, 1.25em, "Wydział Informatyki i Telekomunikacji"))
  ]
  block(spacing: 4pt)[
    #line(length: 100%, stroke: 1.5pt)
  ]

  pad(
    top: 2em,
    align(left)[
      #text(1.25em, "Kierunek: IST")
    ],
  )

  pad(
    top: 5em,
    align(center)[
      #text(weight: 1000, 1.8em, "ZESPOŁOWE PRZEDSIĘWZIĘCIE INFORMATYCZNE")
    ],
  )

  // Project title
  pad(
    top: 4em,
    align(center)[
      #block(text(weight: 1000, 1.65em, title))
    ],
  )

  // Author information.
  pad(
    top: 3em,
    x: 2em,
    grid(
      columns: (1fr,),
      gutter: 1em,
      ..authors.map(author => align(center)[
        #text(1.25em, [#author])
      ]),
    ),
  )

  // Mentor
  pad(
    top: 3em,
    x: 2em,
    align(center)[
      #block(text(1.2em, "Opiekun pracy"))
      #block(spacing: 8pt)[#text(weight: 1000, 1.2em, mentor)]
    ],
  )

  v(1fr)
  line(length: 100%, stroke: 1.5pt)

  pagebreak()

  // Keywords
  stack(
    dir: ltr,
    spacing: 1em,
    text("Słowa kluczowe: "),
    ..keywords.map(keyword => text([#keyword,])),
  )
  line(length: 100%, stroke: 1.5pt)
  align(center)[
    #block(spacing: 4pt)[#text([WROCŁAW #year])]
  ]

  pagebreak()

  outline(indent: 1.5em)
  pagebreak()

  // Main body.
  set par(justify: true)
  set text(hyphenate: false)
  body
}
