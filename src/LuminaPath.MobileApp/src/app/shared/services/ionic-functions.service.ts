import { Injectable, inject } from '@angular/core';
import { ToastController } from '@ionic/angular';
import { ModalController } from '@ionic/angular';

@Injectable({
  providedIn: 'root'
})
export class IonicFunctionsService {
  private toastController = inject(ToastController);
  private modalCtrl = inject(ModalController);


  async openModal(Modal: any, props: any) {
    const modal = await this.modalCtrl.create({
      component: Modal,
      componentProps: props,
    });

    modal.present();

    await modal.onWillDismiss();
  };

  confirm = () => this.modalCtrl.dismiss("confirm");

  async presentToast(message: string, color: 'primary' | 'danger') {
    const toast = await this.toastController.create({
      message: message,
      duration: 2500,
      position: 'bottom',
      color: color
    });

    await toast.present();
  }
}
