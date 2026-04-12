using Xunit;
using przychodnia.Services; 

namespace przychodnia.Tests.Services
{
    public class PasswordHasherTests
    {
        [Fact]
        public void HashPassword_DlaPrawidlowegoHasla_Zwraca64ZnakowyHash()
        {
            
            string haslo = "MojeSuperHaslo123!";

            
            string wynik = PasswordHasher.HashPassword(haslo);

            
            Assert.NotNull(wynik); 
            Assert.Equal(64, wynik.Length); 
            Assert.NotEqual(haslo, wynik); 
        }

        [Fact]
        public void HashPassword_DlaPustegoHasla_ZwracaPustyString()
        {
            
            string pusteHaslo = "";

            
            string wynik = PasswordHasher.HashPassword(pusteHaslo);

           
            Assert.Equal(string.Empty, wynik);
        }

        [Fact]
        public void HashPassword_DlaDwochRoznychHasel_ZwracaRozneWartosci()
        {
            
            string haslo1 = "HasloA";
            string haslo2 = "HasloB";

            
            string hash1 = PasswordHasher.HashPassword(haslo1);
            string hash2 = PasswordHasher.HashPassword(haslo2);

            
            Assert.NotEqual(hash1, hash2); // Dwa różne hasła muszą mieć różny hash
        }
    }
}