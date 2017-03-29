using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

internal class DriveManager
{
    public class parametry
    {
        public string Nazwa;
        public string Atrybuty;
        public int Rozmiar_pliku;
        public int Pierwszy_blok;
        public int Ostatni_blok;
        public parametry(string nazwa, string atrybuty, int rozmiar_pliku, int pierwszy_bolok, int ostatni_blok)
        {
            Nazwa = nazwa;
            Rozmiar_pliku = rozmiar_pliku;
            Atrybuty = atrybuty;
            Pierwszy_blok = pierwszy_bolok;
            Ostatni_blok = ostatni_blok;
        }
    }

    public class Dysk_twardy
    {
        public Dysk_twardy() // Konstruktor dysku twardego
        {
            Dysk = new byte[Liczba_blokow*Rozmiar_bloku];
            FAT = new int[Liczba_blokow];
            InicjalizujFAT();
            InicjalizujDysk();
            Katalog = new List<parametry>();
        }

        private int Liczba_blokow = 32;
        private int Rozmiar_bloku = 16;
        private byte[] Dysk;
        private int wolny_blok = 1;
        private int[] FAT;
        private List<parametry> Katalog;

        #region metody_prywatne

        private void InicjalizujFAT()
            // Inicjalizacja tablicy FAT zerami, oznaczającymi wolne  wolne bloki, pierwszy blok jest zarezerwowany     
        {
            for (int i = 0; i < Liczba_blokow; i++)
            {
                FAT[i] = 0;
            }
            FAT[0] = -1; // Rezerwuję pierwszy blok dysku
        }

        private void InicjalizujDysk()
        {
            for (int i = 0; i < Rozmiar_bloku * Liczba_blokow; i++)
            {
                Dysk[i] = 0;
            }
        }

        private void utworz_plik(string nazwa, string atrybut) // tworzenie pliku w pierwszym wolnym bloku pamięci
        {
            if ((liczba_wolnych_blokow()) >= 1)
            {
                int licznik = 1;
                a:
                if (FAT[licznik] == 0)
                {
                    Katalog.Add(new parametry(nazwa, atrybut, 0, licznik, licznik));
                    FAT[licznik] = -1; // -1 oznacza  zajęte miejsce 
                }
                else
                {
                    licznik++;
                    goto a;
                }
            }
            else
            {
                Console.WriteLine("Brak pamięci");
            }
        }

        private bool nie_zawiera_pl_znaków(string tresc) //Sprawdza czy  tekst zawiera polskie znaki
        {
            string wzor = "^[a-zA-Z0-9!#$%&'()*+,-./:;<=>?/^_{|}~ ]+$"; // Wyrażenia regularne
            Regex r = new Regex(wzor, RegexOptions.IgnoreCase);
            Match m = r.Match(tresc);
            if (m.Success)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void zapis_na_dysk(string zawartosc, int poczatek, int petla)
            //Zapis bezpośredni danych na Dysk ( do tablcy bajtów)    
        {

            byte[] dane = new byte[Liczba_blokow*Rozmiar_bloku];
            dane = System.Text.Encoding.ASCII.GetBytes(zawartosc);

            for (int i = 0; i < petla; i++)
            {
                Dysk[i + (Rozmiar_bloku*poczatek)] = dane[i];
            }
        }

        private int szukaj_wolnego() // szuka wolnego sektora w tablicy FAT
        {
            int wolny = -2;
            int licznik = 0;
            while (wolny != 0)
            {
                if (FAT[licznik] == 0)
                {
                    wolny = 0;
                    wolny_blok = licznik;
                }
                else
                {
                    licznik++;
                }
            }
            return wolny_blok;
        }

        private int liczba_wolnych_blokow()
        {
            int counter = 0;
            for (int i = 1; i < Liczba_blokow; i++)
            {
                if (FAT[i] == 0)
                {
                    counter++;
                }
            }
            return counter;
        }

        private void wyswietlanie_zawartosci_temp(int poczatek)
        {
            string display = "";
            for (int i = poczatek*Rozmiar_bloku; i < ((poczatek*Rozmiar_bloku) + Rozmiar_bloku); i++)
            {
                if (Dysk[i] != 0)
                {
                    display += (char) Dysk[i];
                }
            }
            Console.Write(display);

            if (FAT[poczatek] == -1) return;
            wyswietlanie_zawartosci_temp(FAT[poczatek]);
        }

        private void clear_temp(int poczatek, int poczatek1)
        {
            if (Dysk[poczatek*Rozmiar_bloku] == 0) return;
            for (int i = poczatek*Rozmiar_bloku; i < (poczatek*Rozmiar_bloku + Rozmiar_bloku); i++)
            {
                Dysk[i] = 0;
            }
            if (FAT[poczatek] == -1)
            {
                FAT[poczatek] = 0;
                return;
            }
            clear_temp(FAT[poczatek], poczatek1);
            FAT[poczatek] = 0;
            FAT[poczatek1] = -1;

        }

        private void delete_temp(int poczatek)
        {
            for (int i = poczatek*Rozmiar_bloku; i < (poczatek*Rozmiar_bloku + Rozmiar_bloku); i++)
            {
                Dysk[i] = 0;
            }

            if (FAT[poczatek] == -1)
            {
                FAT[poczatek] = 0;
                return;
            }
            delete_temp(FAT[poczatek]);
            FAT[poczatek] = 0;
        }

        private void dopisz_temp(string nazwa, string zawartosc)
        {
            int poczatek = 1;
            string nowy = "";
            int licznik = 0;
            int licznik2 = 0;
            int temp_wolny_blok = 1;

            foreach (var a in Katalog)
            {
                if (a.Nazwa == nazwa)
                {
                    poczatek = szukaj_wolnego();
                    temp_wolny_blok = szukaj_wolnego();
                }
            }
            double potrzebne_blolki = (zawartosc.Length/decimal.ToDouble(Rozmiar_bloku));

            if ((liczba_wolnych_blokow() + 1) >= Math.Ceiling(potrzebne_blolki) ||
                (liczba_wolnych_blokow() == 0 && Math.Ceiling(potrzebne_blolki) <= 1))
            {
                foreach (char a in zawartosc)
                {
                    if (nowy.Length == Rozmiar_bloku)
                    {                        
                        temp_wolny_blok = szukaj_wolnego();
                        nowy = "";
                        licznik = 0;
                        licznik2++;
                    }
                    nowy += a.ToString();
                    licznik++;

                    if (licznik2 == 0)
                    {
                        zapis_na_dysk(nowy, poczatek, nowy.Length);
                        if (nowy.Length < Rozmiar_bloku)
                        {
                            FAT[poczatek] = -1;
                        }
                        else
                        {
                            if (liczba_wolnych_blokow() == 0 && Math.Ceiling(potrzebne_blolki) <= 1)
                            {
                                FAT[poczatek] = -1;
                            }
                            else
                            {
                                FAT[poczatek] = szukaj_wolnego();
                            }
                        }
                    }
                    else if (licznik2 != 0)
                    {
                        zapis_na_dysk(nowy, wolny_blok, nowy.Length);
                        if (nowy.Length < Rozmiar_bloku)
                        {
                            FAT[wolny_blok] = -1;
                        }
                        else
                        {
                            if (((licznik2 + 1) == Math.Ceiling(potrzebne_blolki)))
                            {
                                FAT[wolny_blok] = -1;
                            }
                            else
                            {
                                FAT[wolny_blok] = szukaj_wolnego();
                            }
                        }
                    }
                }
                foreach (var b in Katalog)
                {
                    if (b.Nazwa == nazwa)
                    {
                        b.Ostatni_blok = temp_wolny_blok;
                    }
                }
                if ((licznik2 == 0 && Math.Floor(potrzebne_blolki) <= 1))
                {
                    FAT[poczatek] = -1;
                }
                if ((licznik2 + 1) == Math.Floor(potrzebne_blolki) && Math.Floor(potrzebne_blolki) > 1)
                {
                    FAT[temp_wolny_blok] = -1;
                }
            }
            else
            {
                Console.WriteLine("Brak pamięci");
            }
        }

        private string _temp = "";
        private string zwracanie_temp1(int poczatek)
        {
            string display = "";
            for (int i = poczatek*Rozmiar_bloku; i < (poczatek*Rozmiar_bloku + Rozmiar_bloku); i++)
            {
                if (Dysk[i] != 0)
                {
                    display += (char) Dysk[i];
                }
            }
            _temp += display;

            if (FAT[poczatek] == -1) return _temp;
            string x = zwracanie_temp1(FAT[poczatek]);
            return x;
        }
        private string zawartosc_pliku(string nazwa)
        {
            int poczatek = 0;
            foreach (var a in Katalog)
            {
                if (a.Nazwa == nazwa)
                {
                    poczatek = a.Pierwszy_blok;
                }
            }
            return zwracanie_temp1(poczatek);
        }

        #endregion

        #region metody_publiczne_uzytkownik
        public void start_systemu()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("                               _/_/_/                    _/            _/_/         _/_/_/");
            Console.WriteLine("                             _/              _/_/       _/_/_/      _/    _/      _/ ");
            Console.WriteLine("                              _/_/        _/_/_/_/     _/    _/    _/    _/        _/_/");
            Console.WriteLine("                                 _/      _/           _/    _/    _/    _/            _/");
            Console.WriteLine("                            _/_/_/        _/_/_/      _/_/_/        _/_/        _/_/_/");
            Console.WriteLine();
            Console.Write("                                               ");
            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 100000000; j++)
                {
                    j++;
                }
                Console.Write("<>");
            }
            Console.Clear();
        }
        public void PokazFAT() // Wyswietlanie tablicy FAT
        {
            Console.WriteLine("FAT:");
            Console.WriteLine();
            Console.WriteLine("     0   1   2   3   4   5   6   7   8   9   A   B   C   D   E   F");
            Console.WriteLine("    --------------------------------------------------------------");
            int licznik = 0;
            Console.Write("0 | ");
            int licznik1 = 0;
            for (int i = 0; i < Liczba_blokow; i++)
            {
                licznik++;
                if (FAT[i] == -1)
                {
                    Console.Write("{0,-4}", "FF");
                }
                else
                {
                    Console.Write("{0,-4}", FAT[i].ToString("X2"));
                }

                if (licznik == 16 && licznik*(licznik1 + 1) != Liczba_blokow)
                {
                    licznik1++;
                    licznik = 0;
                    Console.WriteLine();
                    Console.Write(licznik1 + " | ");
                }
            }
            Console.WriteLine();
        }
        public void Wyswietl_katalog() // Wyswietlanie zawartości katalogu
        {
            if (Katalog.Count == 0)
            {
                Console.WriteLine("Katalog jest pusty.");
            }
            else
            {
                Console.WriteLine("NAZWA            " + "ATRYBUT         " + "ROZMIAR           " +
                                  " PIERWSZY/OSTATNI BLOK ");
                foreach (var a in Katalog)
                {
                    Console.Write("{0,-17}", a.Nazwa);
                    Console.Write("{0,-9}", a.Atrybuty);
                    Console.Write("{0, 10}", a.Rozmiar_pliku);
                    Console.Write(" B");
                    Console.Write("{0, 22}", a.Pierwszy_blok);
                    Console.Write("/");
                    Console.Write("{0, 1}", a.Ostatni_blok);
                    Console.WriteLine();
                }
            }
        }
        public void utworz_plik(string atrybut) // Sprawdzam czy nazwa pliku nie jest dłuższa jak 12 znaków
        {
            Console.WriteLine("Podaj nazwe pliku:");
            string nazwa = Console.ReadLine();

            int wartosc = 2;
            foreach (var a in Katalog)
            {
                if (a.Nazwa == nazwa)
                {
                    wartosc = 1;
                }
            }
            if (nazwa.Length < 13 && wartosc != 1)
            {
                utworz_plik(nazwa, atrybut);
            }

            else if (nazwa.Length > 13 && wartosc != 1)
            {
                Console.WriteLine("Nazwa jest za długa.");
            }
            else if (nazwa.Length < 13 && wartosc == 1)
            {
                Console.WriteLine("Plik o takiej nazwie już istnieje.");
            }
        }
        public void dysk() //Wyswietlanie zawartosci dysku
        {
            int counter = 0;
            int counter1 = 0;
            Console.WriteLine("Blok 0 jest zarezerwowany.");
            int licznik = 0;
            for (int i = 0; i < Rozmiar_bloku*Liczba_blokow; i++)
            {
                if (counter == Rozmiar_bloku *counter1)
                {
                    Console.Write("Blok");
                    Console.Write("{0,4}", counter1);
                    Console.Write(":");
                    counter1++;
                }
                counter++;
              
                  Console.Write("{0,4}", Dysk[i]);  
               
                if (licznik == (Rozmiar_bloku - 1))
                {
                    licznik = -1;
                    Console.WriteLine();
                }
                licznik++;
            }
}  
        public void wpis_zawartosci() // Edycja pliku polegająca na wpisaniu tekstu do pliku - uzytkownik.      
        {
            int poczatek = 0;
            string  zawartosc;
            string nowy = "";
            int licznik = 0;
            int licznik2 = 0;
            int temp_wolny_blok = 1;
            int czy_istnieje = 0;
            int rozmiar_pliku = 0;
            string atrybut = "";

            Console.WriteLine("Podaj nazwe pliku do którego chcesz coś wpisać.");
            string nazwa = Console.ReadLine();

                if (Katalog.Count == 0)
                {
                  Console.WriteLine("Brak pliku o podanej nazwie.");
                  return;
                }           
                foreach (var a in Katalog)
                {
                    if (a.Nazwa == nazwa)
                    {
                    poczatek = a.Pierwszy_blok;
                    atrybut = a.Atrybuty;
                    rozmiar_pliku = a.Rozmiar_pliku;
                    czy_istnieje = 1;
                    }
                }
                if (czy_istnieje == 0)
                {
                    Console.WriteLine("Brak pliku o takiej nazwie.");
                    return;
                }
            
            if (rozmiar_pliku == 0)
            {
                if (atrybut != "TOdczyt")
                {                
                        Console.WriteLine("Trwa wpisywanie zawartosci do pliku.");
                        zawartosc = Console.ReadLine();
                        double potrzebne_blolki = (zawartosc.Length/decimal.ToDouble(Rozmiar_bloku));

                        if (nie_zawiera_pl_znaków(zawartosc))
                        {
                            if ((liczba_wolnych_blokow()) >= Math.Ceiling(potrzebne_blolki) ||
                                (liczba_wolnych_blokow() == 0 && Math.Ceiling(potrzebne_blolki) <= 1))
                            {

                                foreach (char a in zawartosc)
                                {
                                    if (nowy.Length == Rozmiar_bloku)
                                    {
                                        szukaj_wolnego();
                                        temp_wolny_blok = szukaj_wolnego();
                                        nowy = "";
                                        licznik = 0;
                                        licznik2++;
                                    }
                                    nowy += a.ToString();
                                    licznik++;



                                    if (licznik2 == 0)
                                    {
                                        zapis_na_dysk(nowy, poczatek, nowy.Length);
                                        if (nowy.Length < Rozmiar_bloku)
                                        {
                                            FAT[poczatek] = -1;
                                        }
                                        else
                                        {
                                            if (liczba_wolnych_blokow() == 0 && Math.Ceiling(potrzebne_blolki) <= 1)
                                            {
                                                FAT[poczatek] = -1;
                                            }
                                            else
                                            {
                                                FAT[poczatek] = szukaj_wolnego();
                                            }
                                        }
                                        // aktualizacja rozmiaru
                                        foreach (var x in Katalog)
                                        {
                                            if (x.Nazwa == nazwa) x.Rozmiar_pliku = zawartosc.Length;
                                        }
                                    }
                                    else if (licznik2 != 0)
                                    {
                                        zapis_na_dysk(nowy, wolny_blok, nowy.Length);
                                        if (nowy.Length < Rozmiar_bloku)
                                        {
                                            FAT[wolny_blok] = -1;
                                        }
                                        else
                                        {
                                            if (((licznik2 + 1) == Math.Ceiling(potrzebne_blolki)))
                                            {
                                                FAT[wolny_blok] = -1;
                                            }
                                            else
                                            {
                                                FAT[wolny_blok] = szukaj_wolnego();
                                            }
                                        }
                                        // aktualizacja rozmiaru
                                        foreach (var x in Katalog)
                                        {
                                            if (x.Nazwa == nazwa) x.Rozmiar_pliku = zawartosc.Length;
                                        }
                                        foreach (var b in Katalog)
                                        {
                                            if (b.Nazwa == nazwa)
                                            {
                                                b.Ostatni_blok = wolny_blok;

                                            }
                                        }
                                    }
                                }
                                if ((licznik2 == 0 && Math.Floor(potrzebne_blolki) <= 1))
                                {
                                    FAT[poczatek] = -1;
                                }
                                if ((licznik2 + 1) == Math.Floor(potrzebne_blolki) && Math.Floor(potrzebne_blolki) > 1)
                                {
                                    FAT[temp_wolny_blok] = -1;
                                }
                            }
                            else
                            {
                                Console.WriteLine("Brak pamięci.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Błąd!!! Niedozwolone znaki.");
                        }                   
                }
            }
            else
            {
                Console.WriteLine("Plik jest już zapisany. Jeśli chcesz wpisać coś od nowa - wyczyść plik.");
            }
        }
        public void wyswietlanie_zawartosci_pliku()
        {
            int poczatek = 0;
            string nazwa;

            Console.WriteLine("Podaj nazwe pliku ktory chcesz wyswietlić.");
            nazwa = Console.ReadLine();
            foreach (var a in Katalog)
            {
                if (a.Nazwa == nazwa)
                {
                    poczatek = a.Pierwszy_blok;
                }
            }
            wyswietlanie_zawartosci_temp(poczatek);
            Console.WriteLine();
        }    
        public void delete()
        {
            int poczatek = 0;
            int czy_istnieje = 0;
            Console.WriteLine("Podaj nazwe pliku ktory chcesz usunąć.");
            string nazwa = Console.ReadLine();
         
            int licznik = 0;
            if (Katalog.Count == 0)
            {
                Console.WriteLine("Brak pliku o podanej nazwie.");
            }
            else
            {
                foreach (var a in Katalog)
                {
                    if (a.Nazwa == nazwa)
                    {
                        poczatek = a.Pierwszy_blok;
                        czy_istnieje = 1;
                        goto a;
                    }
                    licznik++;
                }
                a:
                if (czy_istnieje == 0)
                {
                    Console.WriteLine("Brak pliku o takiej nazwie.");
                    return;
                }
                Katalog.RemoveAt(licznik);
                delete_temp(poczatek);

            }
        }  
        public void clear()
        {
            int poczatek = 0;
            string nazwa;
            int czy_istnieje = 0;
            Console.WriteLine("Podaj nazwe pliku ktory chcesz wyczyscić.");
            nazwa = Console.ReadLine();
          
            if (Katalog.Count == 0)
            {
                Console.WriteLine("Brak pliku o podanej nazwie.");
            }
            else
            {
                foreach (var a in Katalog)
                {
                    if (a.Nazwa == nazwa)
                    {
                        poczatek = a.Pierwszy_blok;
                        czy_istnieje = 1;
                        a.Rozmiar_pliku = 0;
                        a.Ostatni_blok = a.Pierwszy_blok;
                    }
                }
                if (czy_istnieje == 0)
                {
                    Console.WriteLine("Brak pliku o takiej nazwie.");
                    return;
                }
                clear_temp(poczatek, poczatek);
            }
        }  
        public void dopisz()
        {
            int poczatek = 1;
            int pocz = 1;
            string nazwa, zawartosc;
            int czy_istnieje = 0;
            int counter = 0;
            Console.WriteLine("Podaj nazwe pliku do którego chcesz coś dopisać.");
            nazwa = Console.ReadLine();
            string atrybut = "";
            foreach (var a in Katalog)
            {
                if (a.Nazwa == nazwa)
                {
                    atrybut = a.Atrybuty;
                }
            }

            if (atrybut != "TOdczyt")
            {
                if (Katalog.Count == 0)
                {
                    Console.WriteLine("Brak pliku o podanej nazwie.");
                }
                else
                {
                    foreach (var a in Katalog)
                    {
                        if (a.Nazwa == nazwa)
                        {
                            poczatek = a.Ostatni_blok;
                            pocz = a.Pierwszy_blok;
                            czy_istnieje = 1;
                        }
                    }
                }
                if (czy_istnieje == 0)
                {
                    Console.WriteLine("Brak pliku o takiej nazwie.");
                    return;
                }
                wyswietlanie_zawartosci_temp(pocz);
                zawartosc = Console.ReadLine();

                for (int i = poczatek*Rozmiar_bloku; i < (poczatek*Rozmiar_bloku + Rozmiar_bloku); i++)
                {
                    if (Dysk[i] == 0)
                    {
                        counter++;
                    }
                }
            
                double potrzebne_blolki = (zawartosc.Length/Decimal.ToDouble(Rozmiar_bloku));
                if (nie_zawiera_pl_znaków(zawartosc))
                {
                    if ((liczba_wolnych_blokow()) >= Math.Ceiling(potrzebne_blolki) ||
                        (liczba_wolnych_blokow() == 0 && Math.Ceiling(potrzebne_blolki) <= 1))
                    {
                        foreach (var b in Katalog)
                        {
                            if (b.Nazwa == nazwa)
                            {
                                b.Rozmiar_pliku += zawartosc.Length;
                            }
                        }

                        foreach (char a in zawartosc)
                        {
                                byte[] dane = new byte[Liczba_blokow*Rozmiar_bloku];
                                dane = System.Text.Encoding.ASCII.GetBytes(zawartosc);                       
                                for (int i = 0; i < Rozmiar_bloku; i++)
                                {
                                    if (i == counter)
                                    {
                                        goto a;
                                    }
                                    if (i == zawartosc.Length)
                                    {
                                        return;
                                    }
                                    Dysk[poczatek*Rozmiar_bloku + (Rozmiar_bloku - counter) + i] = dane[i];
                                }
                            
                        }                   
                    a:
                        //========================================================================
                        {
                            string nowy1 = zawartosc.Remove(0, counter);
                            if (nowy1.Length > 0)
                            {
                                FAT[poczatek] = szukaj_wolnego();
                                dopisz_temp(nazwa, nowy1);
                            }
                        }
                        //========================================================================

                    }
                    else
                    {
                        Console.WriteLine("Brak pamięci.");
                    }
                }
                else
                {
                    Console.WriteLine("Błąd!!! Niedozwolone znaki.");
                }
            }
            else
            {
                Console.WriteLine("Plik tylko do odczytu.");
            }
        }
        public void formatuj()
        {
            Katalog.Clear();
            InicjalizujFAT();
            InicjalizujDysk();
        }
        public void funkcje_modulu_FAT()
        {
            Console.WriteLine("mkfile        ->   tworzenie pliku;");
            Console.WriteLine("showfat       ->   wyswietlanie tablicy FAT;");
            Console.WriteLine("dir           ->   wyswietlanie zawartosci katalogu;");
            Console.WriteLine("enter         ->   wpisywanie zawartosci do pliku;");
            Console.WriteLine("add           ->   dopisywanie do pliku;");
            Console.WriteLine("disk          ->   wyswietlanie zawartosci dysku;");
            Console.WriteLine("clear         ->   czyszczenie zawartosci pliku;");
            Console.WriteLine("delete        ->   usuwanie pliku;");
            Console.WriteLine("format        ->   formatowanie całego dysku;");
            Console.WriteLine("load          ->   wczytanie programu asemblerowego;");
            Console.WriteLine("showfile      ->   wyswietlanie zawartości konkretnego pliku;");
            Console.WriteLine("showpartfile  ->   wyswietlanie ciagu znaków (od,ilosc);");
            Console.WriteLine("cls           ->   czyszczenie ekranu");
        }
        #endregion

        #region metody_publiczne_inne_moduly

        //Atrybuty: Edycja lub TOdczyt
        //Nazwa: max 12 znaków
        public void wpis_zawartosci_z_tworzeniem(string nazwa, string atrybut, string zawartosc)// funkcja wpisywania na dysk z jednoczesnym tworzeniem pliku - dla innych modułów 
        {
            //Sprawdzanie nazwy i tworzenie programu
            int wartosc = 2;
            foreach (var a in Katalog)
            {
                if (a.Nazwa == nazwa)
                {
                    wartosc = 1;
                }
            }
            if (nazwa.Length < 13 && wartosc != 1)
            {
                utworz_plik(nazwa, atrybut);
            }

            else if (nazwa.Length > 13 && wartosc != 1)
            {
                Console.WriteLine("Nazwa jest za długa.");
                return;

            }
            else if (nazwa.Length < 13 && wartosc == 1)
            {
                Console.WriteLine("Plik o takiej nazwie już istnieje.");
                return;
            }
            //--------------------------------------------------------------------------
            int poczatek = 0;
            string nowy = "";
            int licznik = 0;
            int licznik2 = 0;
            int temp_wolny_blok = 1;
            int rozmiar_pliku = 0;

            foreach (var a in Katalog)
            {
                if (a.Nazwa == nazwa)
                {
                    poczatek = a.Pierwszy_blok;
                    rozmiar_pliku = a.Rozmiar_pliku;
                }
            }
            if (rozmiar_pliku == 0)
            {
                if (atrybut != "TOdczyt")
                {
                    double potrzebne_blolki = (zawartosc.Length / decimal.ToDouble(Rozmiar_bloku));

                    if (nie_zawiera_pl_znaków(zawartosc))
                    {
                        if ((liczba_wolnych_blokow() + 1) >= Math.Ceiling(potrzebne_blolki) ||
                            (liczba_wolnych_blokow() == 0 && Math.Ceiling(potrzebne_blolki) <= 1))
                        {

                            foreach (char a in zawartosc)
                            {
                                if (nowy.Length == Rozmiar_bloku)
                                {
                                    szukaj_wolnego();
                                    temp_wolny_blok = szukaj_wolnego();
                                    nowy = "";
                                    licznik = 0;
                                    licznik2++;
                                }
                                nowy += a.ToString();
                                licznik++;



                                if (licznik2 == 0)
                                {
                                    zapis_na_dysk(nowy, poczatek, nowy.Length);
                                    if (nowy.Length < Rozmiar_bloku)
                                    {
                                        FAT[poczatek] = -1;
                                    }
                                    else
                                    {
                                        if (liczba_wolnych_blokow() == 0 && Math.Ceiling(potrzebne_blolki) <= 1)
                                        {
                                            FAT[poczatek] = -1;
                                        }
                                        else
                                        {
                                            FAT[poczatek] = szukaj_wolnego();
                                        }
                                    }
                                    // aktualizacja rozmiaru
                                    foreach (var x in Katalog)
                                    {
                                        if (x.Nazwa == nazwa) x.Rozmiar_pliku = zawartosc.Length;
                                    }
                                }
                                else if (licznik2 != 0)
                                {
                                    zapis_na_dysk(nowy, wolny_blok, nowy.Length);
                                    if (nowy.Length < Rozmiar_bloku)
                                    {
                                        FAT[wolny_blok] = -1;
                                    }
                                    else
                                    {
                                        if (((licznik2 + 1) == Math.Ceiling(potrzebne_blolki)))
                                        {
                                            FAT[wolny_blok] = -1;
                                        }
                                        else
                                        {
                                            FAT[wolny_blok] = szukaj_wolnego();
                                        }
                                    }
                                    // aktualizacja rozmiaru
                                    foreach (var x in Katalog)
                                    {
                                        if (x.Nazwa == nazwa) x.Rozmiar_pliku = zawartosc.Length;
                                    }
                                    foreach (var b in Katalog)
                                    {
                                        if (b.Nazwa == nazwa)
                                        {
                                            b.Ostatni_blok = wolny_blok;

                                        }
                                    }
                                }
                            }
                            if ((licznik2 == 0 && Math.Floor(potrzebne_blolki) <= 1))
                            {
                                FAT[poczatek] = -1;
                            }
                            if ((licznik2 + 1) == Math.Floor(potrzebne_blolki) && Math.Floor(potrzebne_blolki) > 1)
                            {
                                FAT[temp_wolny_blok] = -1;
                            }
                        }
                        else
                        {
                            Console.WriteLine("Brak pamięci.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Błąd!!! Niedozwolone znaki.");
                    }
                }
            }
            else
            {
                Console.WriteLine("Plik jest już zapisany. Jeśli chcesz wpisać coś od nowa - wyczyść plik.");
            }
        }
        public void wczytanie_programu(string nazwa, string lokalizacja, string atrybut) //Wczytywanie z dysku np programu asemblerowego
        {
            if (File.Exists(lokalizacja) == false)
            {
                Console.WriteLine("Brak pliku o podanej nazwie");
                return;
            }

            //Sprawdzanie nazwy i tworzenie programu
            int wartosc = 2;
            foreach (var a in Katalog)
            {
                if (a.Nazwa == nazwa)
                {
                    wartosc = 1;
                }
            }
            if (nazwa.Length < 13 && wartosc != 1)
            {
                utworz_plik(nazwa, atrybut);
            }

            else if (nazwa.Length > 13 && wartosc != 1)
            {
                Console.WriteLine("Nazwa jest za długa.");
                return;

            }
            else if (nazwa.Length < 13 && wartosc == 1)
            {
                Console.WriteLine("Plik o takiej nazwie już istnieje.");
                return;
            }
            //--------------------------------------------------------------------------

            int poczatek = 0;
            string zawartosc;
            string nowy = "";
            int licznik = 0;
            int licznik2 = 0;
            int temp_wolny_blok = 1;

            foreach (var a in Katalog)
            {
                if (a.Nazwa == nazwa)
                {
                    poczatek = a.Pierwszy_blok;
                }
            }         
            zawartosc = System.IO.File.ReadAllText(lokalizacja);

            double potrzebne_blolki = (zawartosc.Length / Decimal.ToDouble(Rozmiar_bloku));

            if ((liczba_wolnych_blokow()) >= Math.Ceiling(potrzebne_blolki) || (liczba_wolnych_blokow() == 0 && Math.Ceiling(potrzebne_blolki) <= 1))
            {

                foreach (char a in zawartosc)
                {
                    if (nowy.Length == Rozmiar_bloku)
                    {
                        szukaj_wolnego();
                        temp_wolny_blok = szukaj_wolnego();
                        nowy = "";
                        licznik = 0;
                        licznik2++;
                    }
                    nowy += a.ToString();
                    licznik++;

                    if (licznik2 == 0)
                    {
                        zapis_na_dysk(nowy, poczatek, nowy.Length);
                        if (nowy.Length < Rozmiar_bloku)
                        {
                            FAT[poczatek] = -1;
                        }
                        else
                        {
                            if (liczba_wolnych_blokow() == 0 && Math.Ceiling(potrzebne_blolki) <= 1)
                            {
                                FAT[poczatek] = -1;
                            }
                            else
                            {
                                FAT[poczatek] = szukaj_wolnego();
                            }
                        }
                        // aktualizacja rozmiaru
                        foreach (var x in Katalog)
                        {
                            if (x.Nazwa == nazwa) x.Rozmiar_pliku = zawartosc.Length;
                        }
                    }
                    else if (licznik2 != 0)
                    {
                        zapis_na_dysk(nowy, wolny_blok, nowy.Length);
                        if (nowy.Length < Rozmiar_bloku)
                        {
                            FAT[wolny_blok] = -1;
                        }
                        else
                        {
                            if (((licznik2 + 1) == Math.Ceiling(potrzebne_blolki)))
                            {
                                FAT[wolny_blok] = -1;
                            }
                            else
                            {
                                FAT[wolny_blok] = szukaj_wolnego();
                            }
                        }
                        // aktualizacja rozmiaru
                        foreach (var x in Katalog)
                        {
                            if (x.Nazwa == nazwa) x.Rozmiar_pliku = zawartosc.Length;
                        }
                    }
                }
                if ((licznik2 == 0 && Math.Floor(potrzebne_blolki) <= 1))
                {
                    FAT[poczatek] = -1;
                }
                if ((licznik2 + 1) == Math.Floor(potrzebne_blolki) && Math.Floor(potrzebne_blolki) > 1)
                {
                    FAT[temp_wolny_blok] = -1;
                }
                foreach (var b in Katalog)
                {
                    if (b.Nazwa == nazwa)
                    {
                        b.Ostatni_blok = wolny_blok;

                    }
                }
            }
            else
            {
                delete(nazwa);
                Console.WriteLine("Brak pamięci");
            }
        }
        public string zwracanie_fragmentu(string nazwa, int od, int ilosc)
        {
            string str = zawartosc_pliku(nazwa);
            string zwracany = str.Substring(od, ilosc);
            return zwracany;
        } //zwraca fragment pliku o podanej nazwie
        public void delete(string nazwa)
        {
            int poczatek = 0;
            int czy_istnieje = 0;
            int licznik = 0;
            if (Katalog.Count == 0)
            {
                Console.WriteLine("Brak pliku o podanej nazwie.");
            }
            else
            {
                foreach (var a in Katalog)
                {
                    if (a.Nazwa == nazwa)
                    {
                        poczatek = a.Pierwszy_blok;
                        czy_istnieje = 1;
                        goto a;
                    }
                    licznik++;
                }
            a:
                if (czy_istnieje == 0)
                {
                    Console.WriteLine("Brak pliku o takiej nazwie.");
                    return;
                }
                Katalog.RemoveAt(licznik);
                delete_temp(poczatek);

            }
        } // usuwa plik 
        public void clear(string nazwa)
        {
            int poczatek = 0;       
            int czy_istnieje = 0;
            if (Katalog.Count == 0)
            {
                Console.WriteLine("Brak pliku o podanej nazwie.");
            }
            else
            {
                foreach (var a in Katalog)
                {
                    if (a.Nazwa == nazwa)
                    {
                        poczatek = a.Pierwszy_blok;
                        czy_istnieje = 1;
                        a.Rozmiar_pliku = 0;
                        a.Ostatni_blok = a.Pierwszy_blok;
                    }
                }
                if (czy_istnieje == 0)
                {
                    Console.WriteLine("Brak pliku o takiej nazwie.");
                    return;
                }
                clear_temp(poczatek, poczatek);
            }
        } // już uzyte w funkcji wpis zawartości;
        public void ponowny_zapis(string nazwa, string zawartosc)// funkcja wpisywania na dysk z jednoczesnym tworzeniem pliku - dla innych modułów 
        {
            clear(nazwa);        
            int poczatek = 0;
            string nowy = "";
            int licznik = 0;
            int licznik2 = 0;
            int temp_wolny_blok = 1;
            foreach (var a in Katalog)
            {
                if (a.Nazwa == nazwa)
                {
                    poczatek = a.Pierwszy_blok;
                }
            }        
                    double potrzebne_blolki = (zawartosc.Length / decimal.ToDouble(Rozmiar_bloku));

                    if (nie_zawiera_pl_znaków(zawartosc))
                    {
                        if ((liczba_wolnych_blokow() + 1) >= Math.Ceiling(potrzebne_blolki) ||
                            (liczba_wolnych_blokow() == 0 && Math.Ceiling(potrzebne_blolki) <= 1))
                        {

                            foreach (char a in zawartosc)
                            {
                                if (nowy.Length == Rozmiar_bloku)
                                {
                                    szukaj_wolnego();
                                    temp_wolny_blok = szukaj_wolnego();
                                    nowy = "";
                                    licznik = 0;
                                    licznik2++;
                                }
                                nowy += a.ToString();
                                licznik++;



                                if (licznik2 == 0)
                                {
                                    zapis_na_dysk(nowy, poczatek, nowy.Length);
                                    if (nowy.Length < Rozmiar_bloku)
                                    {
                                        FAT[poczatek] = -1;
                                    }
                                    else
                                    {
                                        if (liczba_wolnych_blokow() == 0 && Math.Ceiling(potrzebne_blolki) <= 1)
                                        {
                                            FAT[poczatek] = -1;
                                        }
                                        else
                                        {
                                            FAT[poczatek] = szukaj_wolnego();
                                        }
                                    }
                                    // aktualizacja rozmiaru
                                    foreach (var x in Katalog)
                                    {
                                        if (x.Nazwa == nazwa) x.Rozmiar_pliku = zawartosc.Length;
                                    }
                                }
                                else if (licznik2 != 0)
                                {
                                    zapis_na_dysk(nowy, wolny_blok, nowy.Length);
                                    if (nowy.Length < Rozmiar_bloku)
                                    {
                                        FAT[wolny_blok] = -1;
                                    }
                                    else
                                    {
                                        if (((licznik2 + 1) == Math.Ceiling(potrzebne_blolki)))
                                        {
                                            FAT[wolny_blok] = -1;
                                        }
                                        else
                                        {
                                            FAT[wolny_blok] = szukaj_wolnego();
                                        }
                                    }
                                    // aktualizacja rozmiaru
                                    foreach (var x in Katalog)
                                    {
                                        if (x.Nazwa == nazwa) x.Rozmiar_pliku = zawartosc.Length;
                                    }
                                    foreach (var b in Katalog)
                                    {
                                        if (b.Nazwa == nazwa)
                                        {
                                            b.Ostatni_blok = wolny_blok;

                                        }
                                    }
                                }
                            }
                            if ((licznik2 == 0 && Math.Floor(potrzebne_blolki) <= 1))
                            {
                                FAT[poczatek] = -1;
                            }
                            if ((licznik2 + 1) == Math.Floor(potrzebne_blolki) && Math.Floor(potrzebne_blolki) > 1)
                            {
                                FAT[temp_wolny_blok] = -1;
                            }
                        }
                        else
                        {
                            Console.WriteLine("Brak pamięci.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Błąd!!! Niedozwolone znaki.");
                    }                   
        }
        #endregion
    }

    // WYWOŁANIA WSZYSTKICH FUNKCJI 'WIERSZ POLECEN'***********************************************************
    private static void Main()
        {
            Dysk_twardy ob = new Dysk_twardy();       
            string znak;
             //ob.start_systemu();
             Console.ForegroundColor = ConsoleColor.White;
             do {
                Console.Write("SebOS:>");               
                znak = Console.ReadLine();
                switch (znak)
                {
                    case "mkfile":
                    {                      
                        ob.utworz_plik("Edycja");                       
                        break;
                    }
                    case "showfat":
                    {
                        ob.PokazFAT();
                        break;
                    }
                    case "dir":
                    {
                        ob.Wyswietl_katalog();
                        break;
                    }
                    case "enter":
                    {
                        ob.wpis_zawartosci();                      
                        break;
                    }
                   case "showpartfile":
                    {
                        Console.WriteLine("Podaj nazwę pliku.");
                        string nazwa = Console.ReadLine();
                        Console.WriteLine("Podaj numer znaku od krótego chcesz wyswietlić plik.");
                        int od = int.Parse(Console.ReadLine());
                        Console.WriteLine("Podaj ile znaków chcesz wyświetlić.");
                        int ilosc = int.Parse(Console.ReadLine());
                        string kawalek = ob.zwracanie_fragmentu(nazwa,od,ilosc);
                        Console.WriteLine(kawalek);
                        break;
                    }
                    case "add":
                    {
                        ob.dopisz();
                        break;
                    }           
                case "disk":
                    {
                        ob.dysk();
                        break;
                    }
                    case "clear":
                    {                
                       ob.clear();                     
                        break;
                    }
                    case "delete":
                    {
                        ob.delete();
                        break;
                    }
                   case "format":
                    {
                        ob.formatuj();
                        break;
                    }
                   case "load":
                    {
                       ob.wczytanie_programu("program1", "program1.txt", "TOdczyt");
                       break;
                    }
                    case "showfile":
                    {
                        ob.wyswietlanie_zawartosci_pliku();
                        break;
                    }
                    case "cls":
                    {
                        Console.Clear();
                        break;
                    }
                    case "help":
                    {
                        ob.funkcje_modulu_FAT();
                        break;
                    }
                    default:
                    {
                        Console.WriteLine("Nie ma takiego polecenia.");
                        break;
                    }
                }
            } while (znak != "x");
        }
    }


