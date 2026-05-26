namespace Tiedragon.NodSystem.Core;

public sealed record FormulaCard(
    string Id,
    string Title,
    string Formula,
    string PlainText,
    string Latex,
    string MathMl,
    IReadOnlyList<string> LevelTags,
    string Description,
    string ExampleNod
);

public static class FormulaCardCatalog
{
    public static IReadOnlyList<FormulaCard> GetDefaultCards()
    {
        return new[]
        {
            new FormulaCard(
                "pythagoras",
                "Pythagoras",
                "c^2 = a^2 + b^2",
                "c = sqrt(a^2 + b^2)",
                @"c = \sqrt{a^2 + b^2}",
                """<math xmlns="http://www.w3.org/1998/Math/MathML"><mrow><mi>c</mi><mo>=</mo><msqrt><mrow><msup><mi>a</mi><mn>2</mn></msup><mo>+</mo><msup><mi>b</mi><mn>2</mn></msup></mrow></msqrt></mrow></math>""",
                new[] { "HAVO", "VWO B", "Meetkunde", "PWS" },
                "Bereken de lengte van de schuine zijde in een rechthoekige driehoek.",
                """
                Name Pythagoras
                input x Zijde a
                input y Zijde b
                output c Schuine zijde
                math sqrt(x^2 + y^2)
                end
                """),

            new FormulaCard(
                "quadratic-formula",
                "Kwadratische vergelijkingen",
                "ax^2 + bx + c = 0, x = (-b +/- sqrt(b^2 - 4ac)) / (2a)",
                "Los ax^2 + bx + c = 0 op met de discriminant D = b^2 - 4ac.",
                @"x=\frac{-b\pm\sqrt{b^2-4ac}}{2a}",
                """<math xmlns="http://www.w3.org/1998/Math/MathML"><mrow><mi>x</mi><mo>=</mo><mfrac><mrow><mo>-</mo><mi>b</mi><mo>&#x00B1;</mo><msqrt><mrow><msup><mi>b</mi><mn>2</mn></msup><mo>-</mo><mn>4</mn><mi>a</mi><mi>c</mi></mrow></msqrt></mrow><mrow><mn>2</mn><mi>a</mi></mrow></mfrac></mrow></math>""",
                new[] { "HAVO B", "VWO B", "Algebra", "Examenbasis" },
                "Los een kwadratische vergelijking op via discriminant en wortelformule. Handig als basiskaart voor havo/vwo.",
                """
                Name Wortelformule notitie
                input text Coefficienten a,b,c
                output x Oplossingen
                end
                """),

            new FormulaCard(
                "exponential-growth",
                "Exponentiele groei",
                "N(t) = N0 * g^t",
                "Waarde na t stappen met beginwaarde N0 en groeifactor g.",
                @"N(t)=N_0\cdot g^t",
                """<math xmlns="http://www.w3.org/1998/Math/MathML"><mrow><mi>N</mi><mo>(</mo><mi>t</mi><mo>)</mo><mo>=</mo><msub><mi>N</mi><mn>0</mn></msub><mo>&#x22C5;</mo><msup><mi>g</mi><mi>t</mi></msup></mrow></math>""",
                new[] { "HAVO A", "HAVO B", "VWO A", "VWO B", "Algebra", "Examenbasis" },
                "Basisvorm voor groei en verval. In formulekaarten nuttig voor procenten, rente, populatie en halfwaardetijd.",
                """
                Name Exponentiele groei notitie
                input text Beginwaarde groeifactor en tijd
                output value Uitkomst
                end
                """),

            new FormulaCard(
                "statistics-mean",
                "Gemiddelde",
                "mean = sum(x_i) / n",
                "Gemiddelde: tel alle waarden op en deel door het aantal waarden.",
                @"\bar{x}=\frac{\sum x_i}{n}",
                """<math xmlns="http://www.w3.org/1998/Math/MathML"><mrow><mover><mi>x</mi><mo>&#x00AF;</mo></mover><mo>=</mo><mfrac><mrow><mo>&#x2211;</mo><msub><mi>x</mi><mi>i</mi></msub></mrow><mi>n</mi></mfrac></mrow></math>""",
                new[] { "HAVO A", "VWO A", "Statistiek", "Wiskunde A", "Examenbasis" },
                "Wiskunde A basis: centrummaat van een dataset. Past bij tabellen, grafieken en onderzoeksdata.",
                """
                Name Gemiddelde dataset
                input text Dataset
                output mean Gemiddelde
                math mean(2,4,4,4,5,5,7,9)
                end
                """),

            new FormulaCard(
                "statistics-median",
                "Mediaan",
                "Me = middle value after sorting",
                "Mediaan: sorteer de waarden en neem de middelste waarde of het gemiddelde van de twee middelste waarden.",
                @"Me=\operatorname{mediaan}(x)",
                """<math xmlns="http://www.w3.org/1998/Math/MathML"><mrow><mi>Me</mi><mo>=</mo><mi>mediaan</mi><mo>(</mo><mi>x</mi><mo>)</mo></mrow></math>""",
                new[] { "HAVO A", "VWO A", "Statistiek", "Wiskunde A", "Examenbasis" },
                "Robuuste centrummaat: minder gevoelig voor uitschieters dan het gemiddelde.",
                """
                Name Mediaan dataset
                input text Dataset
                output median Mediaan
                math median(2,4,4,4,5,5,7,9)
                end
                """),

            new FormulaCard(
                "statistics-stdev-population",
                "Populatie-standaardafwijking",
                "sigma = sqrt(sum((x_i - mean)^2) / n)",
                "Spreidingsmaat voor een volledige populatie: gemiddelde kwadratische afwijking onder de wortel.",
                @"\sigma=\sqrt{\frac{1}{n}\sum (x_i-\bar{x})^2}",
                """<math xmlns="http://www.w3.org/1998/Math/MathML"><mrow><mi>&#x03C3;</mi><mo>=</mo><msqrt><mfrac><mrow><mo>&#x2211;</mo><msup><mrow><mo>(</mo><msub><mi>x</mi><mi>i</mi></msub><mo>-</mo><mover><mi>x</mi><mo>&#x00AF;</mo></mover><mo>)</mo></mrow><mn>2</mn></msup></mrow><mi>n</mi></mfrac></msqrt></mrow></math>""",
                new[] { "HAVO A", "VWO A", "Statistiek", "Wiskunde A", "Examenbasis" },
                "Wiskunde A spreiding. Gebruik stdev voor de hele reeks als populatie; gebruik samplestdev bij een steekproef.",
                """
                Name Populatie standaardafwijking
                input text Dataset
                output sigma Standaardafwijking
                math stdev(2,4,4,4,5,5,7,9)
                end
                """),

            new FormulaCard(
                "probability-combinations",
                "Combinaties",
                "C(n,r) = n! / (r! * (n-r)!)",
                "Aantal manieren om r elementen uit n te kiezen zonder volgorde.",
                @"\binom{n}{r}=\frac{n!}{r!(n-r)!}",
                """<math xmlns="http://www.w3.org/1998/Math/MathML"><mrow><mfenced><mfrac linethickness="0"><mi>n</mi><mi>r</mi></mfrac></mfenced><mo>=</mo><mfrac><mrow><mi>n</mi><mo>!</mo></mrow><mrow><mi>r</mi><mo>!</mo><mo>(</mo><mi>n</mi><mo>-</mo><mi>r</mi><mo>)</mo><mo>!</mo></mrow></mfrac></mrow></math>""",
                new[] { "HAVO A", "VWO A", "Kansrekening", "Wiskunde A", "Examenbasis" },
                "Kansrekening en tellen: kies r uit n zonder volgorde.",
                """
                Name Combinaties
                input n Aantal totaal
                output combinations Aantal combinaties
                math comb(ans,2)
                end
                """),

            new FormulaCard(
                "probability-expected-value",
                "Verwachtingswaarde",
                "E(X) = sum(x_i * p_i)",
                "Gemiddelde uitkomst op lange termijn: vermenigvuldig elke waarde met de bijbehorende kans.",
                @"E(X)=\sum x_i p_i",
                """<math xmlns="http://www.w3.org/1998/Math/MathML"><mrow><mi>E</mi><mo>(</mo><mi>X</mi><mo>)</mo><mo>=</mo><mo>&#x2211;</mo><msub><mi>x</mi><mi>i</mi></msub><msub><mi>p</mi><mi>i</mi></msub></mrow></math>""",
                new[] { "HAVO A", "VWO A", "Kansrekening", "Statistiek", "Wiskunde A" },
                "Verwachtingswaarde bij discrete kansen. Handig voor kansverdelingen en keuzeproblemen.",
                """
                Name Verwachtingswaarde
                input text Waarden en kansen
                output expected Verwachtingswaarde
                math expected(0,0.5,10,0.5)
                end
                """),

            new FormulaCard(
                "trig-right-triangle",
                "Goniometrie rechthoekige driehoek",
                "sin(theta)=opposite/hypotenuse, cos(theta)=adjacent/hypotenuse, tan(theta)=opposite/adjacent",
                "Basisverhoudingen voor sinus, cosinus en tangens in een rechthoekige driehoek.",
                @"\sin(\theta)=\frac{o}{h},\quad \cos(\theta)=\frac{a}{h},\quad \tan(\theta)=\frac{o}{a}",
                """<math xmlns="http://www.w3.org/1998/Math/MathML"><mrow><mi>sin</mi><mo>(</mo><mi>&#x03B8;</mi><mo>)</mo><mo>=</mo><mfrac><mi>o</mi><mi>h</mi></mfrac><mo>,</mo><mi>cos</mi><mo>(</mo><mi>&#x03B8;</mi><mo>)</mo><mo>=</mo><mfrac><mi>a</mi><mi>h</mi></mfrac><mo>,</mo><mi>tan</mi><mo>(</mo><mi>&#x03B8;</mi><mo>)</mo><mo>=</mo><mfrac><mi>o</mi><mi>a</mi></mfrac></mrow></math>""",
                new[] { "HAVO B", "VWO B", "Goniometrie", "2D" },
                "Goniometrie in een rechthoekige driehoek. Goed voor 2D-tekeningen, natuurkunde en PWS-uitleg.",
                """
                Name Goniometrie rechthoekige driehoek notitie
                input text Zijden en hoek
                output ratio Goniometrische verhouding
                end
                """),

            new FormulaCard(
                "linear-function",
                "Lineaire formule",
                "y = a*x + b",
                "Rechte lijn met helling a en startwaarde b.",
                @"y=ax+b",
                """<math xmlns="http://www.w3.org/1998/Math/MathML"><mrow><mi>y</mi><mo>=</mo><mi>a</mi><mi>x</mi><mo>+</mo><mi>b</mi></mrow></math>""",
                new[] { "HAVO", "VWO", "Algebra", "Meetkunde met coordinaten" },
                "Basisvorm van een rechte lijn. Belangrijk voor grafieken, raaklijnen en analytische meetkunde.",
                """
                Name Lineaire formule notitie
                input x X waarde
                input text Helling a en startwaarde b
                output y Y waarde
                end
                """),

            new FormulaCard(
                "distance-between-points",
                "Afstand tussen twee punten",
                "d = sqrt((x2-x1)^2 + (y2-y1)^2)",
                "Afstand tussen P(x1,y1) en Q(x2,y2) in het vlak.",
                @"d(P,Q)=\sqrt{(x_2-x_1)^2+(y_2-y_1)^2}",
                """<math xmlns="http://www.w3.org/1998/Math/MathML"><mrow><mi>d</mi><mo>=</mo><msqrt><mrow><msup><mrow><mo>(</mo><msub><mi>x</mi><mn>2</mn></msub><mo>-</mo><msub><mi>x</mi><mn>1</mn></msub><mo>)</mo></mrow><mn>2</mn></msup><mo>+</mo><msup><mrow><mo>(</mo><msub><mi>y</mi><mn>2</mn></msub><mo>-</mo><msub><mi>y</mi><mn>1</mn></msub><mo>)</mo></mrow><mn>2</mn></msup></mrow></msqrt></mrow></math>""",
                new[] { "HAVO B", "VWO B", "Meetkunde met coordinaten", "Analytische meetkunde", "2D" },
                "Coordinatenmeetkunde: eigenlijk Pythagoras op het verschil tussen twee punten.",
                """
                Name Afstand tussen punten
                input text Punten P en Q
                output d Afstand
                math distance(vec(1,2), vec(4,6))
                end
                """),

            new FormulaCard(
                "midpoint",
                "Midden van twee punten",
                "M = ((x1+x2)/2, (y1+y2)/2)",
                "Middenpunt tussen P(x1,y1) en Q(x2,y2).",
                @"M=\left(\frac{x_1+x_2}{2},\frac{y_1+y_2}{2}\right)",
                """<math xmlns="http://www.w3.org/1998/Math/MathML"><mrow><mi>M</mi><mo>=</mo><mo>(</mo><mfrac><mrow><msub><mi>x</mi><mn>1</mn></msub><mo>+</mo><msub><mi>x</mi><mn>2</mn></msub></mrow><mn>2</mn></mfrac><mo>,</mo><mfrac><mrow><msub><mi>y</mi><mn>1</mn></msub><mo>+</mo><msub><mi>y</mi><mn>2</mn></msub></mrow><mn>2</mn></mfrac><mo>)</mo></mrow></math>""",
                new[] { "HAVO B", "VWO B", "Meetkunde met coordinaten", "Analytische meetkunde", "2D" },
                "Bepaal het punt precies halverwege twee punten.",
                """
                Name Middenpunt
                input text Punten P en Q
                output midpoint Middenpunt
                end
                """),

            new FormulaCard(
                "triangle-area",
                "Oppervlakte driehoek",
                "A = 0.5 * base * height",
                "Oppervlakte van een driehoek met basis en hoogte.",
                @"A=\frac{1}{2}bh",
                """<math xmlns="http://www.w3.org/1998/Math/MathML"><mrow><mi>A</mi><mo>=</mo><mfrac><mn>1</mn><mn>2</mn></mfrac><mi>b</mi><mi>h</mi></mrow></math>""",
                new[] { "HAVO", "VWO", "Meetkunde", "2D", "Examenbasis" },
                "Basale meetkunde: basis maal hoogte gedeeld door twee.",
                """
                Name Oppervlakte driehoek
                input b Basis
                output A Oppervlakte
                math 0.5 * ans * 6
                end
                """),

            new FormulaCard(
                "sine-rule",
                "Sinusregel",
                "a/sin(A) = b/sin(B) = c/sin(C)",
                "Verband tussen zijden en overstaande hoeken in een driehoek.",
                @"\frac{a}{\sin A}=\frac{b}{\sin B}=\frac{c}{\sin C}",
                """<math xmlns="http://www.w3.org/1998/Math/MathML"><mrow><mfrac><mi>a</mi><mrow><mi>sin</mi><mo>(</mo><mi>A</mi><mo>)</mo></mrow></mfrac><mo>=</mo><mfrac><mi>b</mi><mrow><mi>sin</mi><mo>(</mo><mi>B</mi><mo>)</mo></mrow></mfrac><mo>=</mo><mfrac><mi>c</mi><mrow><mi>sin</mi><mo>(</mo><mi>C</mi><mo>)</mo></mrow></mfrac></mrow></math>""",
                new[] { "HAVO B", "VWO B", "Goniometrie" },
                "Sinusregel voor driehoeken zonder rechte hoek. Gebruik zijden met hun overstaande hoeken.",
                """
                Name Sinusregel notitie
                input text Zijden en hoeken van driehoek
                output value Ontbrekende zijde of hoek
                end
                """),

            new FormulaCard(
                "cosine-rule",
                "Cosinusregel",
                "c^2 = a^2 + b^2 - 2*a*b*cos(C)",
                "Algemene driehoeksregel; Pythagoras is het speciale geval C = 90 graden.",
                @"c^2=a^2+b^2-2ab\cos C",
                """<math xmlns="http://www.w3.org/1998/Math/MathML"><mrow><msup><mi>c</mi><mn>2</mn></msup><mo>=</mo><msup><mi>a</mi><mn>2</mn></msup><mo>+</mo><msup><mi>b</mi><mn>2</mn></msup><mo>-</mo><mn>2</mn><mi>a</mi><mi>b</mi><mi>cos</mi><mo>(</mo><mi>C</mi><mo>)</mo></mrow></math>""",
                new[] { "HAVO B", "VWO B", "Goniometrie" },
                "Cosinusregel voor driehoeken. Handig als je twee zijden en de ingesloten hoek kent.",
                """
                Name Cosinusregel notitie
                input text Gegevens van driehoek
                output side Zijde of hoek
                end
                """),

            new FormulaCard(
                "trig-identities",
                "Goniometrische identiteiten",
                "sin^2(x) + cos^2(x) = 1, tan(x) = sin(x)/cos(x)",
                "Kernidentiteiten voor het herleiden van goniometrische uitdrukkingen.",
                @"\sin^2x+\cos^2x=1,\quad \tan x=\frac{\sin x}{\cos x}",
                """<math xmlns="http://www.w3.org/1998/Math/MathML"><mrow><msup><mrow><mi>sin</mi><mo>(</mo><mi>x</mi><mo>)</mo></mrow><mn>2</mn></msup><mo>+</mo><msup><mrow><mi>cos</mi><mo>(</mo><mi>x</mi><mo>)</mo></mrow><mn>2</mn></msup><mo>=</mo><mn>1</mn><mo>,</mo><mi>tan</mi><mo>(</mo><mi>x</mi><mo>)</mo><mo>=</mo><mfrac><mrow><mi>sin</mi><mo>(</mo><mi>x</mi><mo>)</mo></mrow><mrow><mi>cos</mi><mo>(</mo><mi>x</mi><mo>)</mo></mrow></mfrac></mrow></math>""",
                new[] { "HAVO B", "VWO B", "Goniometrie" },
                "Basisidentiteiten voor sinus, cosinus en tangens. Nuttig bij herleiden en vergelijkingen oplossen.",
                """
                Name Goniometrische identiteiten notitie
                input text Hoek x
                output identity Identiteit
                end
                """),

            new FormulaCard(
                "exact-trig-values",
                "Exacte goniometrische waarden",
                "Tabel met 0, 30, 45, 60 en 90 graden",
                "Bekende exacte waarden voor sin, cos en tan bij veelgebruikte hoeken.",
                @"\theta\in\{0^\circ,30^\circ,45^\circ,60^\circ,90^\circ\}",
                """<math xmlns="http://www.w3.org/1998/Math/MathML"><mrow><mi>&#x03B8;</mi><mo>&#x2208;</mo><mo>{</mo><mn>0</mn><mo>&#x00B0;</mo><mo>,</mo><mn>30</mn><mo>&#x00B0;</mo><mo>,</mo><mn>45</mn><mo>&#x00B0;</mo><mo>,</mo><mn>60</mn><mo>&#x00B0;</mo><mo>,</mo><mn>90</mn><mo>&#x00B0;</mo><mo>}</mo></mrow></math>""",
                new[] { "HAVO B", "VWO B", "Goniometrie", "Tabel" },
                "Exacte waarden die vaak terugkomen bij goniometrie. Handig als compacte tabelkaart.",
                """
                Name Exacte goniometrische waarden notitie
                input text Hoek 0 30 45 60 90
                output value Exacte sin cos tan waarde
                end
                """),

            new FormulaCard(
                "circle-equation",
                "Cirkelvergelijking",
                "(x - a)^2 + (y - b)^2 = r^2",
                "Cirkel met middelpunt M(a,b) en straal r.",
                @"(x-a)^2+(y-b)^2=r^2",
                """<math xmlns="http://www.w3.org/1998/Math/MathML"><mrow><msup><mrow><mo>(</mo><mi>x</mi><mo>-</mo><mi>a</mi><mo>)</mo></mrow><mn>2</mn></msup><mo>+</mo><msup><mrow><mo>(</mo><mi>y</mi><mo>-</mo><mi>b</mi><mo>)</mo></mrow><mn>2</mn></msup><mo>=</mo><msup><mi>r</mi><mn>2</mn></msup></mrow></math>""",
                new[] { "HAVO B", "VWO B", "Meetkunde met coordinaten", "2D" },
                "Vergelijking van een cirkel in het vlak. Past bij coördinatenmeetkunde en grafieken.",
                """
                Name Cirkelvergelijking notitie
                input text Middelpunt en straal
                output equation Cirkelvergelijking
                end
                """),

            new FormulaCard(
                "special-right-triangles",
                "Speciale rechthoekige driehoeken",
                "30-60-90: 1:sqrt(3):2, 45-45-90: 1:1:sqrt(2)",
                "Zijdeverhoudingen voor twee veelgebruikte rechthoekige driehoeken.",
                @"30^\circ\!-\!60^\circ\!-\!90^\circ:1:\sqrt3:2,\quad45^\circ\!-\!45^\circ\!-\!90^\circ:1:1:\sqrt2",
                """<math xmlns="http://www.w3.org/1998/Math/MathML"><mrow><mn>30</mn><mo>&#x00B0;</mo><mo>-</mo><mn>60</mn><mo>&#x00B0;</mo><mo>-</mo><mn>90</mn><mo>&#x00B0;</mo><mo>:</mo><mn>1</mn><mo>:</mo><msqrt><mn>3</mn></msqrt><mo>:</mo><mn>2</mn><mo>,</mo><mn>45</mn><mo>&#x00B0;</mo><mo>-</mo><mn>45</mn><mo>&#x00B0;</mo><mo>-</mo><mn>90</mn><mo>&#x00B0;</mo><mo>:</mo><mn>1</mn><mo>:</mo><mn>1</mn><mo>:</mo><msqrt><mn>2</mn></msqrt></mrow></math>""",
                new[] { "HAVO B", "VWO B", "Goniometrie", "2D" },
                "Zijdeverhoudingen voor speciale rechthoekige driehoeken. Helpt bij exacte waarden en schetsen.",
                """
                Name Speciale rechthoekige driehoeken notitie
                input text Type driehoek
                output ratio Zijdeverhouding
                end
                """),

            new FormulaCard(
                "kinetic-energy",
                "Kinetische energie",
                "E = 0.5 * m * v^2",
                "E = 0.5 * m * v^2",
                @"E = \frac{1}{2}mv^2",
                """<math xmlns="http://www.w3.org/1998/Math/MathML"><mrow><mi>E</mi><mo>=</mo><mn>0.5</mn><mo>*</mo><mi>m</mi><mo>*</mo><msup><mi>v</mi><mn>2</mn></msup></mrow></math>""",
                new[] { "HAVO", "VWO", "Physics", "PWS" },
                "Kinetische energie bij massa m en snelheid v.",
                """
                Name Kinetische energie
                mode equation
                input m Massa
                input v Snelheid
                output E Energie
                equation E = 0.5 * m * v^2
                solve E
                end
                """),

            new FormulaCard(
                "derivative-power",
                "Differentieren machtsregel",
                "d/dx x^n = n*x^(n-1)",
                "diff x^n = n*x^(n-1)",
                @"\frac{d}{dx}x^n = nx^{n-1}",
                """<math xmlns="http://www.w3.org/1998/Math/MathML"><mrow><mfrac><mi>d</mi><mrow><mi>d</mi><mi>x</mi></mrow></mfrac><msup><mi>x</mi><mi>n</mi></msup><mo>=</mo><mi>n</mi><msup><mi>x</mi><mrow><mi>n</mi><mo>-</mo><mn>1</mn></mrow></msup></mrow></math>""",
                new[] { "HAVO", "VWO B", "Differentiatie", "CAS-light" },
                "Basisregel voor het differentieren van machten.",
                """
                Name Afgeleide x kwadraat
                input x
                output y
                math diff ans^2
                end
                """),

            new FormulaCard(
                "derivative-sum-rule",
                "Differentieren somregel",
                "d/dx (f(x) + g(x)) = f'(x) + g'(x)",
                "Differentieer een som door elke term apart te differentieren.",
                @"\frac{d}{dx}(f(x)+g(x))=f'(x)+g'(x)",
                """<math xmlns="http://www.w3.org/1998/Math/MathML"><mrow><mfrac><mi>d</mi><mrow><mi>d</mi><mi>x</mi></mrow></mfrac><mo>(</mo><mi>f</mi><mo>(</mo><mi>x</mi><mo>)</mo><mo>+</mo><mi>g</mi><mo>(</mo><mi>x</mi><mo>)</mo><mo>)</mo><mo>=</mo><msup><mi>f</mi><mo>&#x2032;</mo></msup><mo>(</mo><mi>x</mi><mo>)</mo><mo>+</mo><msup><mi>g</mi><mo>&#x2032;</mo></msup><mo>(</mo><mi>x</mi><mo>)</mo></mrow></math>""",
                new[] { "HAVO B", "VWO B", "Differentiatie", "Examenbasis" },
                "Somregel: differentieer elk deel apart. Dezelfde regel geldt voor aftrekken.",
                """
                Name Differentieren somregel notitie
                input text Functie f(x)+g(x)
                output derivative Afgeleide
                end
                """),

            new FormulaCard(
                "derivative-constant-factor",
                "Differentieren constante factorregel",
                "d/dx (c*f(x)) = c*f'(x)",
                "Een constante factor blijft vooraan staan bij differentieren.",
                @"\frac{d}{dx}(c f(x))=c f'(x)",
                """<math xmlns="http://www.w3.org/1998/Math/MathML"><mrow><mfrac><mi>d</mi><mrow><mi>d</mi><mi>x</mi></mrow></mfrac><mo>(</mo><mi>c</mi><mi>f</mi><mo>(</mo><mi>x</mi><mo>)</mo><mo>)</mo><mo>=</mo><mi>c</mi><msup><mi>f</mi><mo>&#x2032;</mo></msup><mo>(</mo><mi>x</mi><mo>)</mo></mrow></math>""",
                new[] { "HAVO B", "VWO B", "Differentiatie", "Examenbasis" },
                "Constante factorregel: een vast getal voor de functie blijft staan.",
                """
                Name Differentieren constante factorregel notitie
                input text Constante maal functie
                output derivative Afgeleide
                end
                """),

            new FormulaCard(
                "derivative-product-rule",
                "Differentieren productregel",
                "d/dx (f*g) = f'*g + f*g'",
                "Gebruik deze regel wanneer twee functies worden vermenigvuldigd.",
                @"(fg)'=f'g+fg'",
                """<math xmlns="http://www.w3.org/1998/Math/MathML"><mrow><mo>(</mo><mi>f</mi><mi>g</mi><msup><mo>)</mo><mo>&#x2032;</mo></msup><mo>=</mo><msup><mi>f</mi><mo>&#x2032;</mo></msup><mi>g</mi><mo>+</mo><mi>f</mi><msup><mi>g</mi><mo>&#x2032;</mo></msup></mrow></math>""",
                new[] { "VWO B", "VWO D", "Differentiatie" },
                "Productregel voor het differentieren van een vermenigvuldiging van twee functies.",
                """
                Name Differentieren productregel notitie
                input text Product f*g
                output derivative Afgeleide
                end
                """),

            new FormulaCard(
                "derivative-quotient-rule",
                "Differentieren quotientregel",
                "d/dx (f/g) = (f'*g - f*g') / g^2",
                "Gebruik deze regel wanneer een functie door een andere functie wordt gedeeld.",
                @"\left(\frac{f}{g}\right)'=\frac{f'g-fg'}{g^2}",
                """<math xmlns="http://www.w3.org/1998/Math/MathML"><mrow><msup><mrow><mo>(</mo><mfrac><mi>f</mi><mi>g</mi></mfrac><mo>)</mo></mrow><mo>&#x2032;</mo></msup><mo>=</mo><mfrac><mrow><msup><mi>f</mi><mo>&#x2032;</mo></msup><mi>g</mi><mo>-</mo><mi>f</mi><msup><mi>g</mi><mo>&#x2032;</mo></msup></mrow><msup><mi>g</mi><mn>2</mn></msup></mfrac></mrow></math>""",
                new[] { "VWO B", "VWO D", "Differentiatie" },
                "Quotientregel voor breuken met functies. Vooral handig bij vwo wiskunde B en verder.",
                """
                Name Differentieren quotientregel notitie
                input text Quotient f/g
                output derivative Afgeleide
                end
                """),

            new FormulaCard(
                "derivative-chain-rule",
                "Differentieren kettingregel",
                "d/dx f(g(x)) = f'(g(x))*g'(x)",
                "Gebruik deze regel voor een functie binnen een andere functie.",
                @"\frac{d}{dx}f(g(x))=f'(g(x))g'(x)",
                """<math xmlns="http://www.w3.org/1998/Math/MathML"><mrow><mfrac><mi>d</mi><mrow><mi>d</mi><mi>x</mi></mrow></mfrac><mi>f</mi><mo>(</mo><mi>g</mi><mo>(</mo><mi>x</mi><mo>)</mo><mo>)</mo><mo>=</mo><msup><mi>f</mi><mo>&#x2032;</mo></msup><mo>(</mo><mi>g</mi><mo>(</mo><mi>x</mi><mo>)</mo><mo>)</mo><msup><mi>g</mi><mo>&#x2032;</mo></msup><mo>(</mo><mi>x</mi><mo>)</mo></mrow></math>""",
                new[] { "HAVO B", "VWO B", "Differentiatie", "Examenbasis" },
                "Kettingregel: differentieer de buitenfunctie en vermenigvuldig met de afgeleide van de binnenfunctie.",
                """
                Name Differentieren kettingregel notitie
                input text Samengestelde functie
                output derivative Afgeleide
                end
                """),

            new FormulaCard(
                "derivative-trig-basic",
                "Differentieren sinus en cosinus",
                "d/dx sin(x)=cos(x), d/dx cos(x)=-sin(x)",
                "Basisafgeleiden voor sinus en cosinus.",
                @"(\sin x)'=\cos x,\quad(\cos x)'=-\sin x",
                """<math xmlns="http://www.w3.org/1998/Math/MathML"><mrow><msup><mrow><mi>sin</mi><mo>(</mo><mi>x</mi><mo>)</mo></mrow><mo>&#x2032;</mo></msup><mo>=</mo><mi>cos</mi><mo>(</mo><mi>x</mi><mo>)</mo><mo>,</mo><msup><mrow><mi>cos</mi><mo>(</mo><mi>x</mi><mo>)</mo></mrow><mo>&#x2032;</mo></msup><mo>=</mo><mo>-</mo><mi>sin</mi><mo>(</mo><mi>x</mi><mo>)</mo></mrow></math>""",
                new[] { "VWO B", "VWO D", "Differentiatie", "Goniometrie" },
                "Basisafgeleiden van sinus en cosinus. Belangrijk bij golven, beweging en periodieke grafieken.",
                """
                Name Differentieren sinus cosinus notitie
                input text Sinus of cosinus functie
                output derivative Afgeleide
                end
                """),

            new FormulaCard(
                "derivative-exp-log",
                "Differentieren exponentieel en logaritme",
                "d/dx e^x=e^x, d/dx ln(x)=1/x",
                "Basisafgeleiden voor e-macht en natuurlijke logaritme.",
                @"(e^x)'=e^x,\quad(\ln x)'=\frac{1}{x}",
                """<math xmlns="http://www.w3.org/1998/Math/MathML"><mrow><msup><mrow><msup><mi>e</mi><mi>x</mi></msup></mrow><mo>&#x2032;</mo></msup><mo>=</mo><msup><mi>e</mi><mi>x</mi></msup><mo>,</mo><msup><mrow><mi>ln</mi><mo>(</mo><mi>x</mi><mo>)</mo></mrow><mo>&#x2032;</mo></msup><mo>=</mo><mfrac><mn>1</mn><mi>x</mi></mfrac></mrow></math>""",
                new[] { "VWO B", "VWO D", "Differentiatie", "Algebra" },
                "Basisafgeleiden voor exponentiele groei en logaritmen.",
                """
                Name Differentieren exponentieel logaritme notitie
                input text Exponentiele of logaritmische functie
                output derivative Afgeleide
                end
                """),

            new FormulaCard(
                "integral-power-rule",
                "Integreren machtsregel",
                "integral x^n dx = x^(n+1)/(n+1) + C",
                "Primitieve van een machtsfunctie voor n ongelijk aan -1.",
                @"\int x^n\,dx=\frac{x^{n+1}}{n+1}+C",
                """<math xmlns="http://www.w3.org/1998/Math/MathML"><mrow><mo>&#x222B;</mo><msup><mi>x</mi><mi>n</mi></msup><mi>d</mi><mi>x</mi><mo>=</mo><mfrac><msup><mi>x</mi><mrow><mi>n</mi><mo>+</mo><mn>1</mn></mrow></msup><mrow><mi>n</mi><mo>+</mo><mn>1</mn></mrow></mfrac><mo>+</mo><mi>C</mi></mrow></math>""",
                new[] { "VWO B", "VWO D", "PWS", "Integralen", "Calculus" },
                "Primitieve van een machtsfunctie. Vooral nuttig voor vwo wiskunde B, PWS en brug naar propedeuse.",
                """
                Name Integreren machtsregel notitie
                input text Functie x^n
                output primitive Primitieve
                end
                """),

            new FormulaCard(
                "integral-sum-rule",
                "Integreren somregel",
                "integral (f + g) dx = integral f dx + integral g dx",
                "Integreer een som door elk deel apart te primitiveren.",
                @"\int(f+g)\,dx=\int f\,dx+\int g\,dx",
                """<math xmlns="http://www.w3.org/1998/Math/MathML"><mrow><mo>&#x222B;</mo><mo>(</mo><mi>f</mi><mo>+</mo><mi>g</mi><mo>)</mo><mi>d</mi><mi>x</mi><mo>=</mo><mo>&#x222B;</mo><mi>f</mi><mi>d</mi><mi>x</mi><mo>+</mo><mo>&#x222B;</mo><mi>g</mi><mi>d</mi><mi>x</mi></mrow></math>""",
                new[] { "VWO B", "VWO D", "Integralen", "Examenbasis" },
                "Somregel voor integralen: splits de formule in losse stukken.",
                """
                Name Integreren somregel notitie
                input text Functie f(x)+g(x)
                output primitive Primitieve
                end
                """),

            new FormulaCard(
                "integral-constant-factor",
                "Integreren constante factorregel",
                "integral c*f dx = c*integral f dx",
                "Een vast getal voor de functie blijft ook bij integreren staan.",
                @"\int c f(x)\,dx=c\int f(x)\,dx",
                """<math xmlns="http://www.w3.org/1998/Math/MathML"><mrow><mo>&#x222B;</mo><mi>c</mi><mi>f</mi><mo>(</mo><mi>x</mi><mo>)</mo><mi>d</mi><mi>x</mi><mo>=</mo><mi>c</mi><mo>&#x222B;</mo><mi>f</mi><mo>(</mo><mi>x</mi><mo>)</mo><mi>d</mi><mi>x</mi></mrow></math>""",
                new[] { "VWO B", "VWO D", "Integralen", "Examenbasis" },
                "Constante factorregel voor integralen: het vaste getal blijft voor de integraal staan.",
                """
                Name Integreren constante factorregel notitie
                input text Constante maal functie
                output primitive Primitieve
                end
                """),

            new FormulaCard(
                "integral-definite-area",
                "Bepaalde integraal als oppervlakte",
                "integraal van a tot b van f(x) dx = oppervlakte",
                "Een bepaalde integraal geeft de getekende oppervlakte tussen grafiek en x-as.",
                @"\int_a^b f(x)\,dx",
                """<math xmlns="http://www.w3.org/1998/Math/MathML"><mrow><msubsup><mo>&#x222B;</mo><mi>a</mi><mi>b</mi></msubsup><mi>f</mi><mo>(</mo><mi>x</mi><mo>)</mo><mi>d</mi><mi>x</mi></mrow></math>""",
                new[] { "VWO B", "VWO D", "Integralen", "Examenbasis" },
                "Gebruik dit voor oppervlakte onder een grafiek op een interval van a tot b.",
                """
                Name Bepaalde integraal oppervlakte notitie
                input text Functie en grenzen a b
                output oppervlakte Oppervlakte
                end
                """),

            new FormulaCard(
                "vector-2d-arrow",
                "2D vectorpijl",
                "v = (x,y), |v| = sqrt(x^2 + y^2)",
                "2D vector van de oorsprong naar punt (x,y)",
                @"\vec{v}=(x,y),\quad |\vec{v}|=\sqrt{x^2+y^2}",
                """<math xmlns="http://www.w3.org/1998/Math/MathML"><mrow><mover><mi>v</mi><mo>&#x2192;</mo></mover><mo>=</mo><mo>(</mo><mi>x</mi><mo>,</mo><mi>y</mi><mo>)</mo><mo>,</mo><mo>|</mo><mover><mi>v</mi><mo>&#x2192;</mo></mover><mo>|</mo><mo>=</mo><msqrt><mrow><msup><mi>x</mi><mn>2</mn></msup><mo>+</mo><msup><mi>y</mi><mn>2</mn></msup></mrow></msqrt></mrow></math>""",
                new[] { "VWO D", "PWS", "Vectoren", "2D grafiek", "Beperkte vector" },
                "Een 2D vector is een pijl vanaf de oorsprong naar (x,y); length(vec(...)) berekent de lengte van die pijl. Voor 3D-visualisatie gebruik je Graph 3D; de geometrie-modus is beschikbaar voor X/Y/Z en NOD-wiskunde.",
                """
                Name 2D vectorpijl notitie
                input x X component
                input y Y component
                output pijl Grafiekpijl
                end
                """),

            new FormulaCard(
                "vector-length-3d",
                "3D vectorlengte",
                "v = (x,y,z), |v| = sqrt(x^2 + y^2 + z^2)",
                "Lengte van een 3D-vector.",
                @"\vec{v}=(x,y,z),\quad |\vec{v}|=\sqrt{x^2+y^2+z^2}",
                """<math xmlns="http://www.w3.org/1998/Math/MathML"><mrow><mover><mi>v</mi><mo>&#x2192;</mo></mover><mo>=</mo><mo>(</mo><mi>x</mi><mo>,</mo><mi>y</mi><mo>,</mo><mi>z</mi><mo>)</mo><mo>,</mo><mo>|</mo><mover><mi>v</mi><mo>&#x2192;</mo></mover><mo>|</mo><mo>=</mo><msqrt><mrow><msup><mi>x</mi><mn>2</mn></msup><mo>+</mo><msup><mi>y</mi><mn>2</mn></msup><mo>+</mo><msup><mi>z</mi><mn>2</mn></msup></mrow></msqrt></mrow></math>""",
                new[] { "VWO D", "PWS", "Vectoren", "3D", "Beperkte vector" },
                "De 3D vectorlengte is de lengte van de pijl vanaf de oorsprong naar (x,y,z). Graph 3D toont de volledige XYZ-pijl; Graph 2D kan dezelfde vector tonen als perspectiefprojectie met (x/z,y/z), of als gewone X/Y-pijl wanneer z nul is.",
                """
                Name 3D vectorlengte
                mode geometry
                input x X component
                input y Y component
                input z Z component
                output lengte Lengte
                math length(vec(x,y,z))
                end
                """),

            new FormulaCard(
                "vector-dot-angle",
                "Inproduct en hoek",
                "a dot b = |a|*|b|*cos(theta)",
                "Inproduct en hoek tussen twee vectoren.",
                @"a\cdot b=\|a\|\|b\|\cos\theta",
                """<math xmlns="http://www.w3.org/1998/Math/MathML"><mrow><mi>a</mi><mo>&#x22C5;</mo><mi>b</mi><mo>=</mo><mo>&#x2016;</mo><mi>a</mi><mo>&#x2016;</mo><mo>&#x2016;</mo><mi>b</mi><mo>&#x2016;</mo><mi>cos</mi><mo>(</mo><mi>&#x03B8;</mi><mo>)</mo></mrow></math>""",
                new[] { "VWO B", "VWO D", "Vectoren", "Meetkunde", "PWS" },
                "Meetkundige betekenis van het inproduct: lengte, richting en hoek.",
                """
                Name Vector inproduct en hoek
                input text Twee vectoren
                output angle Hoek
                math dot(vec(1,2), vec(3,4))
                math angled(vec(1,0), vec(0,1))
                end
                """),

            new FormulaCard(
                "vector-cross-z",
                "Kruisproduct z-component",
                "z(cross(a,b)) = ax*by - ay*bx",
                "2D georienteerde oppervlakte via de z-component van het 3D-kruisproduct.",
                @"(a\times b)_z=a_xb_y-a_yb_x",
                """<math xmlns="http://www.w3.org/1998/Math/MathML"><mrow><msub><mrow><mo>(</mo><mi>a</mi><mo>&#x00D7;</mo><mi>b</mi><mo>)</mo></mrow><mi>z</mi></msub><mo>=</mo><msub><mi>a</mi><mi>x</mi></msub><msub><mi>b</mi><mi>y</mi></msub><mo>-</mo><msub><mi>a</mi><mi>y</mi></msub><msub><mi>b</mi><mi>x</mi></msub></mrow></math>""",
                new[] { "VWO D", "Vectoren", "Meetkunde", "PWS", "Beperkte vector" },
                "Handig voor orientatie en oppervlakte. NOD geeft een vector terug, dus gebruik x/y/z voor een component.",
                """
                Name Kruisproduct z component
                input text Twee vectoren
                output z Z component
                math z(cross(vec(1,0,0), vec(0,1,0)))
                end
                """),

            new FormulaCard(
                "point-line-distance",
                "Afstand punt tot lijn",
                "d = |a*xp + b*yp - c| / sqrt(a^2 + b^2)",
                "Afstand van punt P(xp,yp) tot lijn ax + by = c.",
                @"d(P,l)=\frac{|ax_p+by_p-c|}{\sqrt{a^2+b^2}}",
                """<math xmlns="http://www.w3.org/1998/Math/MathML"><mrow><mi>d</mi><mo>=</mo><mfrac><mrow><mo>|</mo><mi>a</mi><msub><mi>x</mi><mi>p</mi></msub><mo>+</mo><mi>b</mi><msub><mi>y</mi><mi>p</mi></msub><mo>-</mo><mi>c</mi><mo>|</mo></mrow><msqrt><mrow><msup><mi>a</mi><mn>2</mn></msup><mo>+</mo><msup><mi>b</mi><mn>2</mn></msup></mrow></msqrt></mfrac></mrow></math>""",
                new[] { "VWO B", "VWO D", "Meetkunde met coordinaten", "Analytische meetkunde", "2D", "PWS" },
                "Afstand van een punt tot een lijn in 2D. Past goed bij analytische meetkunde en formulekaart-uitleg.",
                """
                Name Afstand punt tot lijn notitie
                input text Punt en lijnwaarden
                output d Afstand
                end
                """),

            new FormulaCard(
                "matrix-2x2-determinant",
                "2x2 matrixdeterminant",
                "det([[a,b],[c,d]]) = a*d - b*c",
                "Determinant van een 2x2 matrix is a*d - b*c.",
                @"\det\begin{pmatrix}a&b\\c&d\end{pmatrix}=ad-bc",
                """<math xmlns="http://www.w3.org/1998/Math/MathML"><mrow><mi>det</mi><mo>(</mo><mfenced><mtable><mtr><mtd><mi>a</mi></mtd><mtd><mi>b</mi></mtd></mtr><mtr><mtd><mi>c</mi></mtd><mtd><mi>d</mi></mtd></mtr></mtable></mfenced><mo>)</mo><mo>=</mo><mi>a</mi><mi>d</mi><mo>-</mo><mi>b</mi><mi>c</mi></mrow></math>""",
                new[] { "VWO D", "PWS", "Lineaire algebra", "Beperkte matrix" },
                "Matrix-onderwerp voor 2x2 matrices. 3x3 matrices werken in NOD-wiskunde met mat3(...), det(...), trace(...) en mget(...); Graph 3D is beschikbaar voor visualisatie.",
                """
                Name 2x2 matrixdeterminant notitie
                input text Matrixwaarden a,b,c,d
                output det Determinant
                end
                """),

            new FormulaCard(
                "circle-integral",
                "Kringintegraal",
                "oint F dot dr",
                "Gesloten lijnintegraal van vectorveld F langs een pad.",
                @"\oint F \cdot dr",
                """<math xmlns="http://www.w3.org/1998/Math/MathML"><mrow><mo>&oint;</mo><mi>F</mi><mo>&#x22C5;</mo><mi>d</mi><mi>r</mi></mrow></math>""",
                new[] { "PWS", "Integralen", "Wiskunde D verdieping", "Propedeuse", "Universiteit" },
                "Verdiepingsonderwerp: vaak niet in standaard wiskunde B. In 2.0 is dit een formulekaart; echte vectorveld/3D-berekening hoort later.",
                """
                Name Kringintegraal notitie
                input text Beschrijving van formule
                end
                """)
        };
    }
}

