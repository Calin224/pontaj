import { Pipe, PipeTransform } from '@angular/core';
import { User } from '../models/user';

@Pipe({
  name: 'name'
})
export class NamePipe implements PipeTransform {

  transform(value: User | null, ...args: unknown[]): unknown {
    if(value?.firstName != null && value.lastName != null){
      return `${value.firstName.toUpperCase()} ${value.lastName.toUpperCase()}`;
    }
    return 'value.email';
  }

}
