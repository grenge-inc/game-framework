using GameFramework.Core;
using SampleGame.Domain.User;

namespace SampleGame.Application.User {
    /// <summary>
    /// ユーザープレイヤー情報管理用リポジトリインターフェース
    /// </summary>
    public interface IUserPlayerRepository {
        /// <summary>
        /// UserPlayer情報の読み込み
        /// </summary>
        IProcess<UserPlayerDto> LoadUserPlayer();

        /// <summary>
        /// 装備武器の保存
        /// </summary>
        IProcess<bool> SaveWeapon(int weaponId);
        
        /// <summary>
        /// 装備頭防具の保存
        /// </summary>
        IProcess<bool> SaveHelmArmor(int armorId);
        
        /// <summary>
        /// 装備体防具の保存
        /// </summary>
        IProcess<bool> SaveBodyArmor(int armorId);
        
        /// <summary>
        /// 装備腕防具の保存
        /// </summary>
        IProcess<bool> SaveArmsArmor(int armorId);
        
        /// <summary>
        /// 装備脚防具の保存
        /// </summary>
        IProcess<bool> SaveLegsArmor(int armorId);
    }
}
