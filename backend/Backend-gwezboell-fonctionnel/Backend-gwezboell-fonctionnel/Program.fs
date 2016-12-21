open System
open System.IO 
open System.ServiceModel
open System.ServiceModel.Web

//Modèle de données
type Couleur = 
| Blanc
| Noir 
| Incolore

type Joueur =
| Joueur of Couleur * string * int // string --> nickname - int = à lui de jouer (1) sinon 0
| Personne
 
type PartieGagnee =
| PartieGagnee of Joueur

type Piece =
| Roi // Roi nécessairement blanc
| Tour of Couleur //couleur de la tour

type Case = 
| CasePiece of Piece
| CaseVide

type Ligne =
| Ligne of Case * Ligne
| LigneVide

type Colonne = 
| Colonne of Ligne * Colonne
| ColonneVide

type Plateau =
| Plateau of Colonne

type Partie =
| Partie of Plateau * Joueur * Joueur  * PartieGagnee // Joueur = qui doit jouer (blanc ou noir)
| Erreur of string
| Vide

let mutable PartieEnCours = Vide // équivalent variable

//Parsers d'objets vers texte
let ConvertirCouleurEnTexte = fun col ->
    match col with
    | Blanc -> "B"
    | Noir -> "N"
    | Incolore -> "Incolore"

let ConvertirJoueurEnTexte = fun j ->
    match j with
    | Joueur(coul, str, tour) -> if Blanc = coul then sprintf "{ \"Couleur\" : \"Blanc\", \"Pseudo\" : \"%s\",  \"Tour\" : %d }" str tour 
                                                 else sprintf "{ \"Couleur\" : \"Noir\", \"Pseudo\" : \"%s\",  \"Tour\" : %d }" str tour 
    | Personne -> "\"Personne\""

let ConvertirPieceEnTexte = fun p ->
    match p with
    | Roi -> "RoiB"
    | Tour(j) -> sprintf "Pion%s" (ConvertirCouleurEnTexte j)

let ConvertirPartieGagneeEnTexte = fun pg ->
    match pg with
    | PartieGagnee(j) -> sprintf "%s" (ConvertirJoueurEnTexte j)

let ConvertirCaseEnTexte = fun ca ->
    match ca with
    | CasePiece(p) -> sprintf "\\\"%s\\\"" (ConvertirPieceEnTexte p)
    | CaseVide -> "\\\"Null\\\""

let rec ConvertirLignesEnTexte = fun li ->
    match li with
    | Ligne(case, ligne) -> match ligne with 
                            | Ligne(c, l) -> sprintf "%s,%s" (ConvertirCaseEnTexte case) (ConvertirLignesEnTexte ligne)
                            | LigneVide -> sprintf "%s" (ConvertirCaseEnTexte case)
    | LigneVide -> ""

let rec ConvertirColonnesEnTexte = fun col ->
    match col with
    | Colonne(l, c) -> match c with 
                       | Colonne(l1, c1) -> sprintf "[%s], %s" (ConvertirLignesEnTexte l) (ConvertirColonnesEnTexte c)
                       | ColonneVide -> sprintf "[%s]" (ConvertirLignesEnTexte l)
    | ColonneVide -> ""

let ConvertirPlateauEnTexte = fun pl ->
    match pl with
    | Plateau(col) -> sprintf "[%s]" (ConvertirColonnesEnTexte col)

let ConvertirPartieEnTexte = fun p ->
    match p with
    | Partie(pl, j1, j2, g) -> sprintf "{\"Plateau\" : \"%s\" , \"Joueur1\" : %s, \"Joueur2\" : %s, \"PartieGagnée\" : %s }" (ConvertirPlateauEnTexte pl) 
                                                                           (ConvertirJoueurEnTexte j1) 
                                                                           (ConvertirJoueurEnTexte j2) 
                                                                           (ConvertirPartieGagneeEnTexte g)
    | Erreur(e) -> sprintf "%s" e

//Match des entrées de l'API
let TexteVersCouleur = fun color ->
    if color = "Blanc" or color = "blanc" then Blanc
    else if color = "Noir" or color = "noir" then Noir
    else Incolore

//Génération du plateau -- Voir pour utiliser des règles?
let EstCeCaseRoi = fun x -> fun y ->
    if x = 5 && y = 5 then true else false

let EstCeTourBlanche = fun x -> fun y -> ((y >=3 && y <= 7 && y <> 5) && x = 5) || ((x >=3 && x <= 7 && x <> 5) && y = 5)

let EstCeTourNoire = fun x -> fun y -> ((y>=4 && y <=6) && (x=1 || x=9)) || ((x>=4 && x <=6) && (y=1 || y=9)) || (x=2 && y=5)|| (y=2 && x=5) || (y=8 && x=5) || (x=8 && y=5)
     
let RecupererPiece = fun x -> fun y ->
    if EstCeCaseRoi x y then CasePiece(Roi) 
    else if EstCeTourBlanche x y then CasePiece(Tour(Blanc))
    else if EstCeTourNoire x y then CasePiece(Tour(Noir))
    else CaseVide

let rec GenererLigne = fun x -> fun y ->
    match y with
    | y when y = 1 -> Ligne(RecupererPiece x y, LigneVide)
    | y when y > 1 -> Ligne(RecupererPiece x y, GenererLigne x (y-1))
     
let rec GenererColonne = fun x -> fun y ->
    match x with
    | x when x = 1 -> Colonne(GenererLigne x y, ColonneVide)
    | x when x > 1 -> Colonne(GenererLigne x y, GenererColonne (x-1) y)

let GenererPlateau = fun x -> Plateau(GenererColonne x x)

let RecupererCouleurCase = fun case ->
    match case with
    | CaseVide -> Incolore
    | CasePiece(Tour(col)) -> col 
    | CasePiece(r) -> Blanc

let RecupererAutreCouleur = fun col ->
    if col = Blanc then Noir else Blanc

let RecupererAutreTour = fun t ->
    if t = 0 then 1 else 0

let AjouterSecondJoueur = fun p -> fun n ->
    match p with
    | Partie(pl, Joueur(col,n1,t1), j2, g) -> if j2 = Personne then PartieEnCours <- Partie(pl, Joueur(col,n1,t1), Joueur(RecupererAutreCouleur(col), n, RecupererAutreTour(t1)), g) 
                                                                    PartieEnCours
                                                               else PartieEnCours

//TODO - Check si partie finie !!
let GenererPartie = fun n -> fun x -> fun couleur ->
    if PartieEnCours = Vide then
        match x with
        | x when x % 2 = 0 -> Erreur("Nombre de case invalide (le nombre doit être impair)")
        | x when x % 2 = 1 -> 
            if Noir = couleur then PartieEnCours <- Partie(GenererPlateau x, Joueur(couleur, n, 1), Personne, PartieGagnee(Personne)) 
            else PartieEnCours <- Partie(GenererPlateau x, Joueur(couleur, n, 0), Personne, PartieGagnee(Personne)) 
            PartieEnCours
    else AjouterSecondJoueur PartieEnCours n 

//Process de la partie
let StringToInt = fun x ->
    let mutable tmp = 0
    if Int32.TryParse(x, &tmp) then tmp else -1

let EstCeUnBonIndex = fun x -> StringToInt x > 0 && StringToInt x < 10

let EstCePartieFinie = fun pg ->
    match pg with
    | PartieGagnee(j) -> j <> Personne 

let EstCePartieEnCours = fun partie ->
    match partie with
    | Partie(p, j1, j2, pg) -> if j2 = Personne then false
                                  else if EstCePartieFinie pg then false
                                  else true 
    | Vide -> false
    | Erreur(s) -> false

let AQuiDeJouer = fun j1 ->
    match j1 with
    | Joueur(col, nick, t) -> if 1 = t then col else RecupererAutreCouleur(col)

let rec RecupererCaseDeLaLigne = fun lig -> fun y ->
    match lig with
    | Ligne(case, ligne) -> if y > 1 then RecupererCaseDeLaLigne ligne (y-1) else case

let rec RecupererLigneDeLaColonne = fun col -> fun x ->
    match col with
    | Colonne(l, c) -> if x > 1 then RecupererLigneDeLaColonne c (x-1) else l

let RecupererCase = fun col -> fun x -> fun y -> RecupererCaseDeLaLigne (RecupererLigneDeLaColonne col x) y

let RecupererCouleurPieceDeLaCase = fun plateau -> fun x -> fun y -> 
    match plateau with
    | Plateau(col) -> RecupererCouleurCase(RecupererCase col x y)

let RecupererJoueur = fun j1 -> fun j2 -> fun couleur ->
    match j1 with
    | Joueur(col, n, t) -> if col = couleur then j1 else j2

let EstCeBonneCaseDeDepart = fun partie -> fun x -> fun y ->
    match partie with
    | Partie(pl, j1, j2, pg) -> (AQuiDeJouer j1) = (RecupererCouleurPieceDeLaCase pl x y)

let rec ParcourirLigne = fun col -> fun x1 -> fun x2 -> fun y ->
    if x1 > x2 then true
    else if RecupererCase col x1 y <> CaseVide then false
    else ParcourirLigne col (x1+1) x2 y

let rec ParcourirColonne = fun col -> fun y1 -> fun y2 -> fun x ->
    if y1 > y2 then true
    else if RecupererCase col x y1 <> CaseVide then false
    else ParcourirColonne col (y1 + 1) y2 x

let EstCeBonCoup = fun partie -> fun x1 -> fun y1 -> fun x2 -> fun y2 ->
    match partie with
    | Partie(Plateau(col), j1, j2, pg) -> if RecupererCase col x2 y2 <> CaseVide then false
                                          else if RecupererCase col x1 y1 <> CasePiece(Roi) && 
                                            ((x2=5 && y2=5) || (x2=1 && y2=1) || (x2=9 && y2=9) || (x2=1 && y2=9) || (x2=9 && y2=1)) then false //Check des cases réservées au roi
                                          else if y1 < y2 && x1 = x2 then ParcourirColonne col (y1+1) y2 x1
                                          else if y1 > y2 && x1 = x2  then ParcourirColonne col y2 (y1-1) x1
                                          else if x1 < x2 && y1 = y2 then ParcourirLigne col (x1+1) x2 y1
                                          else if x1 > x2 && y1 = y2 then ParcourirLigne col x2 (x1-1) y1
                                          else false

let EstCeLeBonJoueur = fun partie -> fun color ->
    match partie with
    | Partie(c, j1, j2, pg) -> if AQuiDeJouer(j1) = TexteVersCouleur color then true else false

let EstCePieceAManger = fun plateau -> fun x1 -> fun y1 -> fun x2 -> fun y2 -> fun x3 -> fun y3 ->
    match plateau with
    | Plateau(col) -> if RecupererCase col x2 y2 = CasePiece(Roi) then false
                      else if x3 = 0 || y3 = 0 || x3 = 10 || y3 = 10 then RecupererCouleurPieceDeLaCase plateau x1 y1 <> RecupererCouleurPieceDeLaCase plateau x2 y2
                      else (RecupererCouleurPieceDeLaCase plateau x1 y1 <> RecupererCouleurPieceDeLaCase plateau x2 y2) && (RecupererCouleurPieceDeLaCase plateau x1 y1 = RecupererCouleurPieceDeLaCase plateau x3 y3)

let MettreAJourJoueur = fun j ->
    match j with
    | Joueur(coul, nick, tour) -> if tour = 1 then Joueur(coul, nick, 0) else Joueur(coul, nick, 1)

let rec MettreAJourLigne = fun ligne -> fun y -> fun case ->
    match ligne with
    | Ligne(c,l) -> if y > 1 then Ligne(c, MettreAJourLigne l (y-1) case) else Ligne(case, l) //On remplace la case

let rec MettreAJourCol = fun col -> fun x -> fun y -> fun case ->
    match col with
    | Colonne(l, c) -> if x > 1 then Colonne(l, MettreAJourCol c (x-1) y case) else Colonne(MettreAJourLigne l y case, c)

let MettreAJourPlateau = fun plateau -> fun x1 -> fun y1 -> fun x2 -> fun y2 ->
    match plateau with
    | Plateau(col) -> Plateau(MettreAJourCol(MettreAJourCol col x2 y2 (RecupererCase col x1 y1)) x1 y1 CaseVide)

let RetirerPieceBas = fun plateau -> fun x -> fun y ->
    if x = 9 then plateau 
    else match plateau with
         | Plateau(col) -> if EstCePieceAManger plateau x y (x+1) y (x+2) y then Plateau(MettreAJourCol col (x+1) y CaseVide) else plateau

let RetirerPieceDroite = fun plateau -> fun x -> fun y ->
    if y = 9 then plateau 
    else match plateau with
         | Plateau(col) -> if EstCePieceAManger plateau x y x (y+1) x (y+2) then Plateau(MettreAJourCol col x (y+1) CaseVide) else plateau

let RetirerPieceGauche = fun plateau -> fun x -> fun y ->
    if y = 1 then plateau 
    else match plateau with
         | Plateau(col) -> if EstCePieceAManger plateau x y x (y-1) x (y-2) then Plateau(MettreAJourCol col x (y-1) CaseVide) else plateau

let RetirerPieceHaut = fun plateau -> fun x -> fun y ->
    if x = 1 then plateau 
    else match plateau with
         | Plateau(col) -> if EstCePieceAManger plateau x y (x-1) y (x-2) y then Plateau(MettreAJourCol col (x-1) y CaseVide) else plateau

let RetirerPieces = fun plateau -> fun x -> fun y -> RetirerPieceHaut (RetirerPieceGauche (RetirerPieceDroite (RetirerPieceBas plateau x y) x y) x y) x y

let EstCeRoiEncercle = fun col -> fun x -> fun y ->
    if (x = 1 || RecupererCase col (x-1) y = CasePiece(Tour(Noir))) && 
       (x = 9 || RecupererCase col (x+1) y = CasePiece(Tour(Noir))) && 
       (y = 1 || RecupererCase col x (y-1) = CasePiece(Tour(Noir))) && 
       (y = 9 || RecupererCase col x (y+1) = CasePiece(Tour(Noir))) then true
    else false

let EstCeNoirGagne = fun col -> fun x -> fun y -> if RecupererCouleurCase(RecupererCase col x y) = Blanc then false
                                                  else if x > 1 && RecupererCase col (x-1) y = CasePiece(Roi) then EstCeRoiEncercle col (x-1) y
                                                  else if y > 1 && RecupererCase col x (y-1) = CasePiece(Roi) then EstCeRoiEncercle col x (y-1)
                                                  else if x < 9 && RecupererCase col (x+1) y = CasePiece(Roi) then EstCeRoiEncercle col (x+1) y
                                                  else if y < 9 && RecupererCase col x (y+1) = CasePiece(Roi) then EstCeRoiEncercle col x (y+1)
                                                  else false

let MettreAJourVainqueur = fun partie -> fun x -> fun y ->
    match partie with
    | Partie(Plateau(col), j1, j2, pg) -> if RecupererCase col x y = CasePiece(Roi) && ((x = 1 && y = 1) || (x = 9 && y = 9) || (x = 1 && y = 9) || (x = 9 && y = 1)) then Partie(Plateau(col), j1, j2, PartieGagnee(RecupererJoueur j1 j2 Blanc))
                                          else if EstCeNoirGagne col x y then Partie(Plateau(col), j1, j2, PartieGagnee(RecupererJoueur j1 j2 Noir))
                                          else partie

let JouerCoup = fun partie -> fun x1 -> fun y1 -> fun x2 -> fun y2 ->
    if false = (EstCeBonneCaseDeDepart partie x1 y1) then "Erreur : Coup invalide (pièce de base incorrecte)"
    else if false = (EstCeBonCoup partie x1 y1 x2 y2) then "Erreur : Coup invalide (Mouvement interdit)"
    else match partie with
         | Partie(plateau, j1, j2, pg) -> PartieEnCours <- MettreAJourVainqueur (Partie(RetirerPieces (MettreAJourPlateau plateau x1 y1 x2 y2) x2 y2, MettreAJourJoueur j1, MettreAJourJoueur j2, pg)) x2 y2
                                          //PartieEnCours <-  PartieEnCours
                                          ConvertirPartieEnTexte(PartieEnCours)
                                       

let initialiserPartie = fun nick -> fun color ->
    if TexteVersCouleur color <> Incolore then ConvertirPartieEnTexte(GenererPartie nick 9 (TexteVersCouleur color))
    else "Erreur : Couleur invalide"

let jouerPartie = fun partie -> fun color -> fun x1 -> fun y1 -> fun x2 -> fun y2 ->
    if TexteVersCouleur color = Incolore then "Erreur : Couleur invalide"
    else if false = EstCeUnBonIndex x1  then "Erreur : Valeur d'index incorrect (x1)"
    else if false = EstCeUnBonIndex y1  then "Erreur : Valeur d'index incorrect (y1)"
    else if false = EstCeUnBonIndex x2  then "Erreur : Valeur d'index incorrect (x2)"
    else if false = EstCeUnBonIndex y2  then "Erreur : Valeur d'index incorrect (y2)"
    else if false = EstCePartieEnCours partie then "Erreur : Partie non instanciée"
    else if false = EstCeLeBonJoueur partie color then "Erreur : Ce n'est pas à ce joueur de jouer"
    else JouerCoup partie (StringToInt x1) (StringToInt y1) (StringToInt x2) (StringToInt y2)
    
//Gestion du EndPoint
[<ServiceContract>]
type MyContract() =
    [<OperationContract>]
    [<WebGet(UriTemplate="{nick}/{color}/")>]
    member this.GetPartie(nick:string, color:string) : Stream = upcast new MemoryStream(System.Text.Encoding.UTF8.GetBytes(initialiserPartie nick color))
    [<WebGet(UriTemplate="{nick}/{color}/{x1}:{y1}:{x2}:{y2}")>]
    member this.Get(nick:string, color:string, x1:string, y1: string, x2:string, y2:string) : Stream = upcast new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jouerPartie PartieEnCours color x1 y1 x2 y2))

let Main() =
    let address = "http://localhost:64385/"
    let host = new WebServiceHost(typeof<MyContract>, new Uri(address))
    host.AddServiceEndpoint(typeof<MyContract>, new WebHttpBinding(), "") 
        |> ignore
    host.Open()


    printfn "Server running at %s" address
    printfn "Press a key to close server"
    System.Console.ReadKey() |> ignore
    host.Close()

Main()