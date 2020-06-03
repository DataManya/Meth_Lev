using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Diagnostics;

//гибрид
namespace Met_DamLevDist_MetrLev
{
    class Program
    {
        const string alphabet = "ОЕАИУЭЮЯПСТРКЛМНБВГДЖЗЙФХЦЧШЩЫЁ";//алфавит кроме исключаемых букв
        const string voiced = "БЗДВГ";//звонкие
        const string unvoiced = "ПСТФК";//глухие
        const string consonants = "ПСТКБВГДЖЗФХЦЧШЩ";//согласные, перед которыми звонкие оглушаются (кроме Л Н М Р
        const string vowels = "ОЮЕЭЯЁЫ";//образец гласных
        const string vowelsReplace = "АУИИАИА";// замена гласных


        static string MetaphoneRu(string str)
        {
            if ((str == null) || (str.Length == 0))
                return "";

            //в верхний регистр
            str = str.ToUpper();
            //новая строка
            var sb = new StringBuilder(" ");
            //оставили только символы из alf
            for (int i = 0; i < str.Length; i++)
            {
                if (alphabet.Contains(str[i]))//содержится ли str в алфавите
                    sb.Append(str[i]);//исключаем ь, ъ
            }
            var new_str = sb.ToString();


            //Оглушаем последний символ, если он - звонкий согласный Б З Д В Г
            var voicedIndex = voiced.IndexOf(new_str.LastChar());
            if (voicedIndex >= 0)
                new_str = new_str.ReplaceLastChar(unvoiced[voicedIndex], 1);//заменяем глухим П С Т Ф К
            new_str = new_str.Trim(' ');//убираем пробелы если они есть
            var oldCh = ' ';
            string res = "";
            for (int i = 0; i < new_str.Length; i++)
            {
                var ch = new_str[i];
                if (ch != oldCh)
                {
                    //блок согласных
                    if (consonants.Contains(ch))
                    {
                        //если больше 1 буквы
                        if (i > 0)
                        {
                            if ((oldCh == 'Т' || oldCh == 'Д') && ch == 'С')
                            {
                                oldCh = 'Ц';
                                res = res.ReplaceLastChar(oldCh, 1);
                                continue;
                            }

                            else
                            {
                                var voicedIndexMiddle = voiced.IndexOf(oldCh);//если предыдущий символ звонкий
                                if (voicedIndexMiddle >= 0)
                                {
                                    res = res.ReplaceLastChar(unvoiced[voicedIndexMiddle], 1);
                                    res += ch;
                                    oldCh = ch;
                                    continue;
                                }
                            }

                            res += ch;
                            oldCh = ch;
                            continue;
                        }

                        else
                        {
                            res += ch;
                            oldCh = ch;
                            continue;
                        }
                    }
                    //иначе гласная
                    else
                    {
                        var vowelIndex = vowels.IndexOf(ch);
                        if (vowelIndex >= 0)
                        {
                            if (i > 0)
                            {
                                if ((oldCh == 'Й' || oldCh == 'И') && (ch == 'О' || ch == 'Е'))
                                {
                                    oldCh = 'И';
                                    res = res.ReplaceLastChar(oldCh, 1);
                                    continue;
                                }
                                else//Если не буквосочетания с гласной, а просто гласная
                                {
                                    res += vowelsReplace[vowelIndex];//заменяем гласную
                                    oldCh = ch;
                                    continue;
                                }
                            }

                            else
                            {
                                res += vowelsReplace[vowelIndex];
                                oldCh = ch;
                                continue;
                            }
                        }

                        else
                        {
                            res += ch;
                            oldCh = ch;
                            continue;
                        }
                    }
                }
            }
            return res.ToLower();
        }


        static int DamerauLevenshteinDistance(string string1, string string2)
        {
            if (string1 == null) return string2.Length;
            if (string2 == null) return string1.Length;
            var n = string1.Length + 1;
            var m = string2.Length + 1;

            var array = new int[n, m];

            for (var i = 0; i < n; i++)
            {
                array[i, 0] = i;
            }

            for (var j = 0; j < m; j++)
            {
                array[0, j] = j;
            }

            for (var i = 1; i < n; i++)
            {
                for (var j = 1; j < m; j++)
                {
                    var cost = string1[i - 1] == string2[j - 1] ? 0 : 1;

                    array[i, j] = Math.Min(Math.Min(array[i - 1, j] + 1,          // удаление
                                                    array[i, j - 1] + 1),         // вставка
                                                    array[i - 1, j - 1] + cost); // замена

                    if (i > 1 && j > 1
                        && string1[i - 1] == string2[j - 2]
                        && string1[i - 2] == string2[j - 1])
                    {
                        array[i, j] = Math.Min(array[i, j],
                                               array[i - 2, j - 2] + cost); // перестановка
                    }
                }
            }
            return array[n - 1, m - 1];
        }

        public static float GetSimilarity(string string1, string string2, int distance)
        {
            float maxLen = string1.Length;
            if (maxLen < string2.Length)
                maxLen = string2.Length;
            if (maxLen == 0)
                return 1;
            else
                return (1 - (distance / maxLen));
        }

        static string GetSimilarString(string input, StreamReader words)
        {
            string minDistWord = "ОШИБКА";
            using (words)
            {
                var minDist = int.MaxValue;
                float similar = 0;

                do
                {
                    String line = words.ReadLine();
                    var metaphoneLine = MetaphoneRu(line);
                    var currentDist = DamerauLevenshteinDistance(input, metaphoneLine);

                    if (currentDist < minDist)
                    {
                        var similarString = GetSimilarity(input, metaphoneLine, currentDist);
                        if (Helpers.CompareFloat(similarString, similar))
                        {
                            minDist = currentDist;
                            minDistWord = line;
                            similar = similarString;
                        }
                    }
                }
                while (!words.EndOfStream);
            }
            return minDistWord;
        }


        static void Main()
        {
            StreamReader f = new StreamReader("input.txt", Encoding.GetEncoding("windows-1251"));
            StreamWriter g = new StreamWriter("output.txt");

            string input = f.ReadLine();
            string[] inputWords = input.Split(' ');

            Stopwatch swatch = new Stopwatch();
            swatch.Start();
            for (int i = 0; i < inputWords.Length; i++)
            {
                var metaphone = MetaphoneRu(inputWords[i]);
                g.WriteLine(GetSimilarString(metaphone, new StreamReader("words.txt")));
            }
            swatch.Stop();
            Console.WriteLine(swatch.ElapsedMilliseconds + "ms");

            f.Close();
            g.Close();
        }
    }

    static class Helpers
    {
        public static string ReplaceLastChar(this string s, char c, int k)
        {
            return s.Substring(0, s.Length - k) + c;//обрезаем строку без последнего элемента
        }

        public static char LastChar(this string s)
        {
            return s[s.Length - 1];
        }

        public static bool CompareFloat(float x, float y)
        {
            return x > y;
        }
    }
}
