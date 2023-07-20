import { toastController } from '@ionic/vue';

export async function presentToast(message: string, color: 'primary' | 'danger') {
    const toast = await toastController.create({
      message: message,
      duration: 1500,
      position: 'bottom',
      color: color
    });

    await toast.present();
}